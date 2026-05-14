using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure.Identity;
using Survey.Infrastructure.Persistence;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService(
	SurveyDbContext dbContext,
	UserManager<ApplicationUser> userManager) : ISurveyAdministrationService, ISurveyExperienceService
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	private readonly SurveyDbContext _dbContext = dbContext;
	private readonly UserManager<ApplicationUser> _userManager = userManager;

	public async Task<IReadOnlyList<SurveyDefinitionListItem>> GetSurveyDefinitionsAsync(bool archivedOnly = false, CancellationToken cancellationToken = default)
	{
		return await _dbContext.SurveyDefinitions
			.AsNoTracking()
			.Where(definition => definition.IsArchived == archivedOnly)
			.OrderBy(definition => definition.Name)
			.Select(definition => new SurveyDefinitionListItem
			{
				Id = definition.Id,
				Name = definition.Name,
				Description = definition.Description,
				VersionCount = definition.Versions.Count,
				IsArchived = definition.IsArchived,
				UpdatedUtc = definition.UpdatedUtc
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<SurveyDefinitionEditModel> GetSurveyDefinitionAsync(int? id, CancellationToken cancellationToken = default)
	{
		if (!id.HasValue)
		{
			return new SurveyDefinitionEditModel();
		}

		var entity = await _dbContext.SurveyDefinitions
			.AsNoTracking()
			.FirstOrDefaultAsync(definition => definition.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested survey was not found.");

		return new SurveyDefinitionEditModel
		{
			Id = entity.Id,
			Name = entity.Name,
			Description = entity.Description,
			IsArchived = entity.IsArchived
		};
	}

	public async Task<int> SaveSurveyDefinitionAsync(SurveyDefinitionEditModel model, CancellationToken cancellationToken = default)
	{
		SurveyDefinition entity;
		if (model.Id.HasValue)
		{
			entity = await _dbContext.SurveyDefinitions.FirstOrDefaultAsync(definition => definition.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested survey was not found.");
			entity.Update(model.Name, model.Description);
		}
		else
		{
			entity = new SurveyDefinition(model.Name, model.Description);
			_dbContext.SurveyDefinitions.Add(entity);
		}

		entity.SetArchived(model.IsArchived);

		await _dbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	public async Task<IReadOnlyList<SurveyVersionListItem>> GetSurveyVersionsAsync(int? surveyDefinitionId, bool archivedOnly = false, CancellationToken cancellationToken = default)
	{
		var query = _dbContext.SurveyVersions
			.AsNoTracking()
			.Include(version => version.SurveyDefinition)
			.Include(version => version.Sections)
			.Include(version => version.Assignments)
			.AsQueryable();

		query = archivedOnly
			? query.Where(version => version.IsArchived || version.SurveyDefinition.IsArchived)
			: query.Where(version => !version.IsArchived && !version.SurveyDefinition.IsArchived);

		if (surveyDefinitionId.HasValue)
		{
			query = query.Where(version => version.SurveyDefinitionId == surveyDefinitionId.Value);
		}

		return await query
			.OrderBy(version => version.SurveyDefinition.Name)
			.ThenBy(version => version.VersionNumber)
			.Select(version => new SurveyVersionListItem
			{
				Id = version.Id,
				SurveyDefinitionId = version.SurveyDefinitionId,
				SurveyName = version.SurveyDefinition.Name,
				DisplayName = version.DisplayName,
				VersionNumber = version.VersionNumber,
				IsPublished = version.IsPublished,
				IsArchived = version.IsArchived || version.SurveyDefinition.IsArchived,
				IsLocked = version.Assignments.Any(),
				SectionCount = version.Sections.Count,
				AssignmentCount = version.Assignments.Count
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<SurveyVersionEditModel> GetSurveyVersionAsync(int? id, int? surveyDefinitionId, CancellationToken cancellationToken = default)
	{
		if (!id.HasValue)
		{
			var surveyOptions = await GetSurveyDefinitionOptionsAsync(surveyDefinitionId, cancellationToken);
			var selectedSurveyId = surveyDefinitionId ?? surveyOptions.Select(option => TryParseInt(option.Value)).FirstOrDefault(value => value > 0);
			var nextVersionNumber = selectedSurveyId > 0
				? await GetNextVersionNumberAsync(selectedSurveyId, cancellationToken)
				: 1;
			var surveyName = surveyOptions.FirstOrDefault(option => option.Value == selectedSurveyId.ToString())?.Label ?? "Survey";

			return new SurveyVersionEditModel
			{
				SurveyDefinitionId = selectedSurveyId,
				DisplayName = $"{surveyName} v{nextVersionNumber}",
				VersionNumber = nextVersionNumber,
				SurveyOptions = surveyOptions
			};
		}

		var entity = await _dbContext.SurveyVersions
			.AsNoTracking()
			.Include(version => version.SurveyDefinition)
			.Include(version => version.Assignments)
			.FirstOrDefaultAsync(version => version.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested survey version was not found.");
		var editSurveyOptions = await GetSurveyDefinitionOptionsAsync(entity.SurveyDefinitionId, cancellationToken);

		return new SurveyVersionEditModel
		{
			Id = entity.Id,
			SurveyDefinitionId = entity.SurveyDefinitionId,
			DisplayName = entity.DisplayName,
			VersionNumber = entity.VersionNumber,
			IsPublished = entity.IsPublished,
			IsArchived = entity.IsArchived,
			IsLocked = entity.Assignments.Any(),
			SurveyOptions = editSurveyOptions
		};
	}

	public async Task<int> SaveSurveyVersionAsync(SurveyVersionEditModel model, CancellationToken cancellationToken = default)
	{
		await EnsureSurveyDefinitionExistsAsync(model.SurveyDefinitionId, cancellationToken);

		SurveyVersion entity;
		if (model.Id.HasValue)
		{
			entity = await _dbContext.SurveyVersions
				.Include(version => version.Assignments)
				.FirstOrDefaultAsync(version => version.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested survey version was not found.");

			if (entity.Assignments.Any())
			{
				if (entity.SurveyDefinitionId != model.SurveyDefinitionId
					|| !string.Equals(entity.DisplayName, model.DisplayName, StringComparison.Ordinal)
					|| entity.VersionNumber != model.VersionNumber
					|| entity.IsPublished != model.IsPublished)
				{
					throw new InvalidOperationException("This survey version is locked because it has already been assigned.");
				}
			}
			else
			{
				entity.Update(model.DisplayName, model.VersionNumber, model.IsPublished);
			}
		}
		else
		{
			entity = new SurveyVersion(model.SurveyDefinitionId, model.DisplayName, model.VersionNumber, model.IsPublished);
			_dbContext.SurveyVersions.Add(entity);
		}

		entity.SetArchived(model.IsArchived);

		await _dbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	public async Task<int> CloneSurveyVersionAsync(int surveyVersionId, CancellationToken cancellationToken = default)
	{
		var source = await _dbContext.SurveyVersions
			.AsNoTracking()
			.Include(version => version.SurveyDefinition)
			.Include(version => version.Sections)
				.ThenInclude(section => section.Questions)
					.ThenInclude(question => question.Options)
			.FirstOrDefaultAsync(version => version.Id == surveyVersionId, cancellationToken)
			?? throw new InvalidOperationException("The requested survey version was not found.");

		var nextVersionNumber = await GetNextVersionNumberAsync(source.SurveyDefinitionId, cancellationToken);
		var clone = new SurveyVersion(
			source.SurveyDefinitionId,
			$"{source.SurveyDefinition.Name} v{nextVersionNumber}",
			nextVersionNumber,
			false);

		_dbContext.SurveyVersions.Add(clone);
		await _dbContext.SaveChangesAsync(cancellationToken);

		foreach (var sourceSection in source.Sections.OrderBy(section => section.SortOrder))
		{
			var clonedSection = new SurveySection(clone.Id, sourceSection.Title, sourceSection.Description, sourceSection.SortOrder);
			_dbContext.SurveySections.Add(clonedSection);
			await _dbContext.SaveChangesAsync(cancellationToken);

			foreach (var sourceQuestion in sourceSection.Questions.OrderBy(question => question.SortOrder))
			{
				var clonedQuestion = new SurveyQuestion(
					clonedSection.Id,
					sourceQuestion.Prompt,
					sourceQuestion.HelpText,
					sourceQuestion.Type,
					sourceQuestion.IsRequired,
					sourceQuestion.SortOrder);
				_dbContext.SurveyQuestions.Add(clonedQuestion);
				await _dbContext.SaveChangesAsync(cancellationToken);

				foreach (var sourceOption in sourceQuestion.Options.OrderBy(option => option.SortOrder))
				{
					_dbContext.QuestionOptions.Add(new QuestionOption(clonedQuestion.Id, sourceOption.Label, sourceOption.SortOrder));
				}

				await _dbContext.SaveChangesAsync(cancellationToken);
			}
		}

		return clone.Id;
	}

	public async Task<IReadOnlyList<SurveySectionListItem>> GetSurveySectionsAsync(int surveyVersionId, CancellationToken cancellationToken = default)
	{
		return await _dbContext.SurveySections
			.AsNoTracking()
			.Where(section => section.SurveyVersionId == surveyVersionId)
			.Include(section => section.SurveyVersion)
				.ThenInclude(version => version.SurveyDefinition)
			.Include(section => section.Questions)
			.OrderBy(section => section.SortOrder)
			.ThenBy(section => section.Title)
			.Select(section => new SurveySectionListItem
			{
				Id = section.Id,
				SurveyVersionId = section.SurveyVersionId,
				SurveyDefinitionId = section.SurveyVersion.SurveyDefinitionId,
				VersionName = section.SurveyVersion.DisplayName,
				Title = section.Title,
				SortOrder = section.SortOrder,
				QuestionCount = section.Questions.Count,
				IsLocked = section.SurveyVersion.Assignments.Any()
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<SurveySectionEditModel> GetSurveySectionAsync(int? id, int? surveyVersionId, CancellationToken cancellationToken = default)
	{
		if (!id.HasValue)
		{
			var versionOptions = await GetSurveyVersionOptionsAsync(includeUnpublished: true, includeVersionId: surveyVersionId, cancellationToken: cancellationToken);
			var selectedVersionId = surveyVersionId ?? versionOptions.Select(option => TryParseInt(option.Value)).FirstOrDefault(value => value > 0);
			var versionContext = selectedVersionId > 0
				? await _dbContext.SurveyVersions
					.AsNoTracking()
					.Include(version => version.Assignments)
					.FirstOrDefaultAsync(version => version.Id == selectedVersionId, cancellationToken)
				: null;

			return new SurveySectionEditModel
			{
				SurveyDefinitionId = versionContext?.SurveyDefinitionId ?? 0,
				SurveyVersionId = selectedVersionId,
				IsLocked = versionContext?.Assignments.Any() == true,
				VersionName = versionContext?.DisplayName ?? string.Empty,
				SurveyVersionOptions = versionOptions
			};
		}

		var entity = await _dbContext.SurveySections
			.AsNoTracking()
			.Include(section => section.SurveyVersion)
				.ThenInclude(version => version.Assignments)
			.Include(section => section.SurveyVersion)
				.ThenInclude(version => version.SurveyDefinition)
			.FirstOrDefaultAsync(section => section.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested section was not found.");
		var editVersionOptions = await GetSurveyVersionOptionsAsync(includeUnpublished: true, includeVersionId: entity.SurveyVersionId, cancellationToken: cancellationToken);

		return new SurveySectionEditModel
		{
			Id = entity.Id,
			SurveyDefinitionId = entity.SurveyVersion.SurveyDefinitionId,
			SurveyVersionId = entity.SurveyVersionId,
			Title = entity.Title,
			Description = entity.Description,
			SortOrder = entity.SortOrder,
			IsLocked = entity.SurveyVersion.Assignments.Any(),
			VersionName = entity.SurveyVersion.DisplayName,
			SurveyVersionOptions = editVersionOptions
		};
	}

	public async Task<int> SaveSurveySectionAsync(SurveySectionEditModel model, CancellationToken cancellationToken = default)
	{
		if (model.Id.HasValue)
		{
			var section = await _dbContext.SurveySections.FirstOrDefaultAsync(entity => entity.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested section was not found.");
			await EnsureVersionEditableAsync(section.SurveyVersionId, cancellationToken);
			section.Update(model.Title, model.Description, model.SortOrder);
			await _dbContext.SaveChangesAsync(cancellationToken);
			return section.Id;
		}

		await EnsureVersionEditableAsync(model.SurveyVersionId, cancellationToken);
		var entity = new SurveySection(model.SurveyVersionId, model.Title, model.Description, model.SortOrder);
		_dbContext.SurveySections.Add(entity);
		await _dbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	public async Task<IReadOnlyList<SurveyQuestionListItem>> GetSurveyQuestionsAsync(int surveySectionId, CancellationToken cancellationToken = default)
	{
		return await _dbContext.SurveyQuestions
			.AsNoTracking()
			.Where(question => question.SurveySectionId == surveySectionId)
			.Include(question => question.SurveySection)
				.ThenInclude(section => section.SurveyVersion)
					.ThenInclude(version => version.Assignments)
			.Include(question => question.Options)
			.OrderBy(question => question.SortOrder)
			.ThenBy(question => question.Prompt)
			.Select(question => new SurveyQuestionListItem
			{
				Id = question.Id,
				SurveySectionId = question.SurveySectionId,
				SectionTitle = question.SurveySection.Title,
				Prompt = question.Prompt,
				Type = question.Type,
				IsRequired = question.IsRequired,
				SortOrder = question.SortOrder,
				OptionCount = question.Options.Count,
				IsLocked = question.SurveySection.SurveyVersion.Assignments.Any()
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<SurveyQuestionEditModel> GetSurveyQuestionAsync(int? id, int? surveySectionId, CancellationToken cancellationToken = default)
	{
		if (!id.HasValue)
		{
			var sectionOptions = await GetSurveySectionOptionsAsync(surveySectionId, cancellationToken);
			var selectedSectionId = surveySectionId ?? sectionOptions.Select(option => TryParseInt(option.Value)).FirstOrDefault(value => value > 0);
			var sectionContext = selectedSectionId > 0
				? await _dbContext.SurveySections
					.AsNoTracking()
					.Include(section => section.SurveyVersion)
						.ThenInclude(version => version.Assignments)
					.FirstOrDefaultAsync(section => section.Id == selectedSectionId, cancellationToken)
				: null;

			return new SurveyQuestionEditModel
			{
				SurveyDefinitionId = sectionContext?.SurveyVersion.SurveyDefinitionId ?? 0,
				SurveyVersionId = sectionContext?.SurveyVersionId ?? 0,
				SurveySectionId = selectedSectionId,
				IsLocked = sectionContext?.SurveyVersion.Assignments.Any() == true,
				SupportsOptions = SupportsOptions(SurveyQuestionType.YesNo),
				SurveySectionOptions = sectionOptions
			};
		}

		var entity = await _dbContext.SurveyQuestions
			.AsNoTracking()
			.Include(question => question.SurveySection)
				.ThenInclude(section => section.SurveyVersion)
					.ThenInclude(version => version.Assignments)
			.FirstOrDefaultAsync(question => question.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested question was not found.");
		var editSectionOptions = await GetSurveySectionOptionsAsync(entity.SurveySectionId, cancellationToken);

		return new SurveyQuestionEditModel
		{
			Id = entity.Id,
			SurveyDefinitionId = entity.SurveySection.SurveyVersion.SurveyDefinitionId,
			SurveyVersionId = entity.SurveySection.SurveyVersionId,
			SurveySectionId = entity.SurveySectionId,
			Prompt = entity.Prompt,
			HelpText = entity.HelpText,
			Type = entity.Type,
			IsRequired = entity.IsRequired,
			SortOrder = entity.SortOrder,
			IsLocked = entity.SurveySection.SurveyVersion.Assignments.Any(),
			SupportsOptions = SupportsOptions(entity.Type),
			SurveySectionOptions = editSectionOptions
		};
	}

	public async Task<int> SaveSurveyQuestionAsync(SurveyQuestionEditModel model, CancellationToken cancellationToken = default)
	{
		if (model.Id.HasValue)
		{
			var question = await _dbContext.SurveyQuestions.FirstOrDefaultAsync(entity => entity.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested question was not found.");
			var versionId = await GetVersionIdForQuestionAsync(question.Id, cancellationToken);
			await EnsureVersionEditableAsync(versionId, cancellationToken);
			question.Update(model.Prompt, model.HelpText, model.Type, model.IsRequired, model.SortOrder);
			await _dbContext.SaveChangesAsync(cancellationToken);
			return question.Id;
		}

		var parentVersionId = await GetVersionIdForSectionAsync(model.SurveySectionId, cancellationToken);
		await EnsureVersionEditableAsync(parentVersionId, cancellationToken);
		var entity = new SurveyQuestion(model.SurveySectionId, model.Prompt, model.HelpText, model.Type, model.IsRequired, model.SortOrder);
		_dbContext.SurveyQuestions.Add(entity);
		await _dbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	public async Task<IReadOnlyList<QuestionOptionListItem>> GetQuestionOptionsAsync(int surveyQuestionId, CancellationToken cancellationToken = default)
	{
		return await _dbContext.QuestionOptions
			.AsNoTracking()
			.Where(option => option.SurveyQuestionId == surveyQuestionId)
			.Include(option => option.SurveyQuestion)
				.ThenInclude(question => question.SurveySection)
					.ThenInclude(section => section.SurveyVersion)
						.ThenInclude(version => version.Assignments)
			.OrderBy(option => option.SortOrder)
			.ThenBy(option => option.Label)
			.Select(option => new QuestionOptionListItem
			{
				Id = option.Id,
				SurveyQuestionId = option.SurveyQuestionId,
				QuestionPrompt = option.SurveyQuestion.Prompt,
				Label = option.Label,
				SortOrder = option.SortOrder,
				IsLocked = option.SurveyQuestion.SurveySection.SurveyVersion.Assignments.Any()
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<QuestionOptionEditModel> GetQuestionOptionAsync(int? id, int? surveyQuestionId, CancellationToken cancellationToken = default)
	{
		if (!id.HasValue)
		{
			var questionContext = surveyQuestionId.HasValue
				? await _dbContext.SurveyQuestions
					.AsNoTracking()
					.Include(question => question.SurveySection)
						.ThenInclude(section => section.SurveyVersion)
							.ThenInclude(version => version.Assignments)
					.FirstOrDefaultAsync(question => question.Id == surveyQuestionId.Value, cancellationToken)
				: null;
			var selectedQuestionId = surveyQuestionId ?? 0;
			var questionOptions = await GetQuestionSelectOptionsAsync(selectedQuestionId > 0 ? selectedQuestionId : null, cancellationToken);
			if (selectedQuestionId <= 0)
			{
				selectedQuestionId = questionOptions.Select(option => TryParseInt(option.Value)).FirstOrDefault(value => value > 0);
			}

			if (questionContext is null && selectedQuestionId > 0)
			{
				questionContext = await _dbContext.SurveyQuestions
					.AsNoTracking()
					.Include(question => question.SurveySection)
						.ThenInclude(section => section.SurveyVersion)
							.ThenInclude(version => version.Assignments)
					.FirstOrDefaultAsync(question => question.Id == selectedQuestionId, cancellationToken);
			}

			return new QuestionOptionEditModel
			{
				SurveyDefinitionId = questionContext?.SurveySection.SurveyVersion.SurveyDefinitionId ?? 0,
				SurveyVersionId = questionContext?.SurveySection.SurveyVersionId ?? 0,
				SurveySectionId = questionContext?.SurveySectionId ?? 0,
				SurveyQuestionId = selectedQuestionId > 0 ? selectedQuestionId : questionOptions.Select(option => TryParseInt(option.Value)).FirstOrDefault(value => value > 0),
				QuestionPrompt = questionContext?.Prompt ?? string.Empty,
				QuestionType = questionContext?.Type ?? SurveyQuestionType.YesNo,
				SupportsOptions = questionContext is not null && SupportsOptions(questionContext.Type),
				IsLocked = questionContext?.SurveySection.SurveyVersion.Assignments.Any() == true,
				SurveyQuestionOptions = questionOptions
			};
		}

		var entity = await _dbContext.QuestionOptions
			.AsNoTracking()
			.Include(option => option.SurveyQuestion)
				.ThenInclude(question => question.SurveySection)
					.ThenInclude(section => section.SurveyVersion)
						.ThenInclude(version => version.Assignments)
			.FirstOrDefaultAsync(option => option.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested option was not found.");
		var editQuestionOptions = await GetQuestionSelectOptionsAsync(entity.SurveyQuestionId, cancellationToken);

		return new QuestionOptionEditModel
		{
			Id = entity.Id,
			SurveyDefinitionId = entity.SurveyQuestion.SurveySection.SurveyVersion.SurveyDefinitionId,
			SurveyVersionId = entity.SurveyQuestion.SurveySection.SurveyVersionId,
			SurveySectionId = entity.SurveyQuestion.SurveySectionId,
			SurveyQuestionId = entity.SurveyQuestionId,
			QuestionPrompt = entity.SurveyQuestion.Prompt,
			QuestionType = entity.SurveyQuestion.Type,
			Label = entity.Label,
			SortOrder = entity.SortOrder,
			IsLocked = entity.SurveyQuestion.SurveySection.SurveyVersion.Assignments.Any(),
			SupportsOptions = SupportsOptions(entity.SurveyQuestion.Type),
			SurveyQuestionOptions = editQuestionOptions
		};
	}

	public async Task<int> SaveQuestionOptionAsync(QuestionOptionEditModel model, CancellationToken cancellationToken = default)
	{
		var question = await _dbContext.SurveyQuestions
			.AsNoTracking()
			.Include(entity => entity.Options)
			.FirstOrDefaultAsync(entity => entity.Id == model.SurveyQuestionId, cancellationToken)
			?? throw new InvalidOperationException("The selected question was not found.");

		if (question.Type is not SurveyQuestionType.SingleChoice and not SurveyQuestionType.MultiSelect)
		{
			throw new InvalidOperationException("Options can only be added to single-choice or multi-select questions.");
		}

		var versionId = await GetVersionIdForQuestionAsync(model.SurveyQuestionId, cancellationToken);
		await EnsureVersionEditableAsync(versionId, cancellationToken);

		if (model.Id.HasValue)
		{
			var option = await _dbContext.QuestionOptions.FirstOrDefaultAsync(entity => entity.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested option was not found.");
			option.Update(model.Label, model.SortOrder);
			await _dbContext.SaveChangesAsync(cancellationToken);
			return option.Id;
		}

		var entity = new QuestionOption(model.SurveyQuestionId, model.Label, model.SortOrder);
		_dbContext.QuestionOptions.Add(entity);
		await _dbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	public async Task<IReadOnlyList<PersonListItem>> GetPeopleAsync(CancellationToken cancellationToken = default)
	{
		return await _dbContext.People
			.AsNoTracking()
			.OrderBy(person => person.LastName)
			.ThenBy(person => person.FirstName)
			.Select(person => new PersonListItem
			{
				Id = person.Id,
				FirstName = person.FirstName,
				MiddleName = person.MiddleName,
				LastName = person.LastName,
				PostalCode = person.PostalCode,
				Email = person.Email,
				PhoneNumber = person.PhoneNumber
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<PersonEditModel> GetPersonAsync(int? id, CancellationToken cancellationToken = default)
	{
		if (!id.HasValue)
		{
			return await BuildPersonEditModelAsync(null, cancellationToken);
		}

		var entity = await _dbContext.People
			.AsNoTracking()
			.Include(person => person.PostalAddress)
			.FirstOrDefaultAsync(person => person.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested person was not found.");

		return await BuildPersonEditModelAsync(entity, cancellationToken);
	}

	public async Task<int> SavePersonAsync(PersonEditModel model, CancellationToken cancellationToken = default)
	{
		var resolvedAddress = await ResolveOrCreatePostalAddressAsync(
			model.CountryId,
			model.StateProvinceId,
			model.CountyId,
			model.AddressLine1,
			model.AddressLine2,
			model.City,
			model.PostalCode,
			cancellationToken);

		Person entity;
		if (model.Id.HasValue)
		{
			entity = await _dbContext.People.FirstOrDefaultAsync(person => person.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested person was not found.");
			entity.Update(
				model.FirstName,
				model.MiddleName,
				model.LastName,
				resolvedAddress.PostalAddress.Id,
				model.AddressLine1,
				model.AddressLine2,
				model.City,
				resolvedAddress.StateProvince.Code,
				model.PostalCode,
				model.PhoneNumber,
				model.BestTimeToContact,
				model.Email,
				resolvedAddress.Country.Name);
		}
		else
		{
			entity = new Person(
				model.FirstName,
				model.MiddleName,
				model.LastName,
				resolvedAddress.PostalAddress.Id,
				model.AddressLine1,
				model.AddressLine2,
				model.City,
				resolvedAddress.StateProvince.Code,
				model.PostalCode,
				model.PhoneNumber,
				model.BestTimeToContact,
				model.Email,
				resolvedAddress.Country.Name);
			_dbContext.People.Add(entity);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	public async Task<IReadOnlyList<SurveyAssignmentListItem>> GetAssignmentsAsync(CancellationToken cancellationToken = default)
	{
		var now = DateTimeOffset.UtcNow;

		var assignments = await _dbContext.SurveyAssignments
			.AsNoTracking()
			.Include(assignment => assignment.Person)
			.Include(assignment => assignment.SurveyVersion)
				.ThenInclude(version => version.SurveyDefinition)
			.Include(assignment => assignment.Response)
			.Select(assignment => new SurveyAssignmentListItem
			{
				Id = assignment.Id,
				PersonName = BuildFullName(assignment.Person.FirstName, assignment.Person.MiddleName, assignment.Person.LastName),
				SurveyName = assignment.SurveyVersion.SurveyDefinition.Name,
				VersionName = assignment.SurveyVersion.DisplayName,
				PublicToken = assignment.PublicToken,
				ExpiresAtUtc = assignment.ExpiresAtUtc,
				CreatedUtc = assignment.CreatedUtc,
				IsCompleted = assignment.Response != null,
				IsExpired = assignment.ExpiresAtUtc.HasValue && assignment.ExpiresAtUtc.Value <= now
			})
			.ToListAsync(cancellationToken);

		return assignments
			.OrderByDescending(assignment => assignment.CreatedUtc)
			.ToList();
	}

	public async Task<SurveyAssignmentEditModel> GetAssignmentAsync(int? id, CancellationToken cancellationToken = default)
	{
		var personOptions = await GetPersonSelectOptionsAsync(cancellationToken);

		if (!id.HasValue)
		{
			var availableVersionOptions = await GetSurveyVersionOptionsAsync(includeUnpublished: false, includeVersionId: null, cancellationToken: cancellationToken);
			return new SurveyAssignmentEditModel
			{
				PersonId = personOptions.Select(option => TryParseInt(option.Value)).FirstOrDefault(value => value > 0),
				SurveyVersionId = availableVersionOptions.Select(option => TryParseInt(option.Value)).FirstOrDefault(value => value > 0),
				PublicToken = GeneratePublicToken(),
				PersonOptions = personOptions,
				SurveyVersionOptions = availableVersionOptions
			};
		}

		var entity = await _dbContext.SurveyAssignments
			.AsNoTracking()
			.Include(assignment => assignment.Response)
			.FirstOrDefaultAsync(assignment => assignment.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested assignment was not found.");
		var versionOptions = await GetSurveyVersionOptionsAsync(includeUnpublished: false, includeVersionId: entity.SurveyVersionId, cancellationToken: cancellationToken);

		return new SurveyAssignmentEditModel
		{
			Id = entity.Id,
			PersonId = entity.PersonId,
			SurveyVersionId = entity.SurveyVersionId,
			ExpiresAtUtc = entity.ExpiresAtUtc,
			PublicToken = entity.PublicToken,
			IsCompleted = entity.Response is not null,
			PersonOptions = personOptions,
			SurveyVersionOptions = versionOptions
		};
	}

	public async Task<int> SaveAssignmentAsync(SurveyAssignmentEditModel model, string? createdByUserId, CancellationToken cancellationToken = default)
	{
		await EnsurePersonExistsAsync(model.PersonId, cancellationToken);

		var version = await _dbContext.SurveyVersions
			.AsNoTracking()
			.Include(entity => entity.SurveyDefinition)
			.FirstOrDefaultAsync(entity => entity.Id == model.SurveyVersionId, cancellationToken)
			?? throw new InvalidOperationException("The selected survey version was not found.");

		if (!version.IsPublished)
		{
			throw new InvalidOperationException("Only published survey versions can be assigned.");
		}

		if (version.IsArchived || version.SurveyDefinition.IsArchived)
		{
			throw new InvalidOperationException("Archived survey versions cannot be assigned.");
		}

		if (model.Id.HasValue)
		{
			var assignment = await _dbContext.SurveyAssignments
				.Include(entity => entity.Response)
				.FirstOrDefaultAsync(entity => entity.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested assignment was not found.");

			if (assignment.Response is not null)
			{
				throw new InvalidOperationException("Completed assignments can no longer be edited.");
			}

			assignment.Update(string.IsNullOrWhiteSpace(model.PublicToken) ? GeneratePublicToken() : model.PublicToken, model.ExpiresAtUtc);
			await _dbContext.SaveChangesAsync(cancellationToken);
			return assignment.Id;
		}

		var entity = new SurveyAssignment(
			model.PersonId,
			model.SurveyVersionId,
			string.IsNullOrWhiteSpace(model.PublicToken) ? GeneratePublicToken() : model.PublicToken,
			model.ExpiresAtUtc,
			createdByUserId);
		_dbContext.SurveyAssignments.Add(entity);
		await _dbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	public async Task<IReadOnlyList<SurveyResponseListItem>> GetResponsesAsync(CancellationToken cancellationToken = default)
	{
		var responses = await _dbContext.SurveyResponses
			.AsNoTracking()
			.Include(response => response.SurveyAssignment)
				.ThenInclude(assignment => assignment.Person)
			.Include(response => response.SurveyAssignment)
				.ThenInclude(assignment => assignment.SurveyVersion)
					.ThenInclude(version => version.SurveyDefinition)
			.ToListAsync(cancellationToken);

		var userLookup = await GetUserDisplayLookupAsync(responses
			.Where(response => !string.IsNullOrWhiteSpace(response.SubmittedByUserId))
			.Select(response => response.SubmittedByUserId!)
			.Distinct()
			.ToList(), cancellationToken);

		return BuildSurveyResponseListItems(responses, userLookup);
	}

	public async Task<IReadOnlyList<SurveyResponseListItem>> GetUnmappedResponsesAsync(string postalCode, CancellationToken cancellationToken = default)
	{
		var normalizedFilter = postalCode?.Trim() ?? string.Empty;
		var responses = await _dbContext.SurveyResponses
			.AsNoTracking()
			.Include(response => response.SurveyAssignment)
				.ThenInclude(assignment => assignment.Person)
			.Include(response => response.SurveyAssignment)
				.ThenInclude(assignment => assignment.SurveyVersion)
					.ThenInclude(version => version.SurveyDefinition)
			.ToListAsync(cancellationToken);
		var userLookup = await GetUserDisplayLookupAsync(responses
			.Where(response => !string.IsNullOrWhiteSpace(response.SubmittedByUserId))
			.Select(response => response.SubmittedByUserId!)
			.Distinct()
			.ToList(), cancellationToken);
		var primaryCountyLookup = await GetPrimaryCountyLookupByZipAsync(cancellationToken);

		return BuildSurveyResponseListItems(responses, userLookup)
			.Where(item =>
			{
				var itemPostalCode = item.PostalCode?.Trim();
				var resolvedCounty = !string.IsNullOrWhiteSpace(itemPostalCode)
					&& primaryCountyLookup.TryGetValue(itemPostalCode, out var county)
					? county
					: null;
				if (resolvedCounty?.CountyFips is not null)
				{
					return false;
				}

				return string.Equals(normalizedFilter, "Missing", StringComparison.OrdinalIgnoreCase)
					? string.IsNullOrWhiteSpace(itemPostalCode)
					: string.Equals(itemPostalCode, normalizedFilter, StringComparison.OrdinalIgnoreCase);
			})
			.ToList();
	}

	public async Task<SurveyResponseDetailModel?> GetResponseAsync(int id, CancellationToken cancellationToken = default)
	{
		var response = await _dbContext.SurveyResponses
			.AsNoTracking()
			.Include(entity => entity.SurveyAssignment)
				.ThenInclude(assignment => assignment.Person)
			.Include(entity => entity.Answers)
				.ThenInclude(answer => answer.SurveyQuestion)
					.ThenInclude(question => question.Options)
			.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

		if (response is null)
		{
			return null;
		}

		var userLookup = await GetUserDisplayLookupAsync(
			string.IsNullOrWhiteSpace(response.SubmittedByUserId) ? [] : [response.SubmittedByUserId],
			cancellationToken);

		return new SurveyResponseDetailModel
		{
			Id = response.Id,
			SurveyAssignmentId = response.SurveyAssignmentId,
			PersonId = response.SurveyAssignment.PersonId,
			RespondentPostalAddressId = response.RespondentPostalAddressId,
			SurveyName = response.SurveyNameSnapshot,
			VersionName = response.SurveyVersionNameSnapshot,
			RespondentName = BuildFullName(response.RespondentFirstName, response.RespondentMiddleName, response.RespondentLastName),
			HomeAddress = FormatAddressOrFallback(
				response.RespondentAddressLine1,
				response.RespondentAddressLine2,
				response.RespondentCity,
				response.RespondentState,
				response.RespondentPostalCode,
				response.RespondentHomeAddress),
			PostalCode = response.RespondentPostalCode ?? PostalCodeNormalizer.Extract(response.RespondentHomeAddress),
			CountyName = response.RespondentCountyNameSnapshot,
			PhoneNumber = response.RespondentPhoneNumber,
			BestTimeToContact = response.RespondentBestTimeToContact,
			Email = response.RespondentEmail,
			SubmittedByLabel = BuildSubmittedByLabel(response, userLookup),
			SubmittedUtc = response.SubmittedUtc,
			Answers = response.Answers
				.OrderBy(answer => answer.Id)
				.Select(answer => new SurveyResponseAnswerDetailModel
				{
					Question = answer.QuestionPromptSnapshot,
					Answer = FormatAnswer(answer)
				})
				.ToList()
		};
	}

	private List<SurveyResponseListItem> BuildSurveyResponseListItems(
		IReadOnlyList<SurveyResponse> responses,
		IReadOnlyDictionary<string, string> userLookup)
	{
		return responses
			.OrderByDescending(response => response.SubmittedUtc)
			.Select(response => new SurveyResponseListItem
			{
				Id = response.Id,
				SurveyAssignmentId = response.SurveyAssignmentId,
				PersonId = response.SurveyAssignment.PersonId,
				SurveyName = response.SurveyNameSnapshot,
				VersionName = response.SurveyVersionNameSnapshot,
				RespondentName = BuildFullName(response.RespondentFirstName, response.RespondentMiddleName, response.RespondentLastName),
				PostalCode = response.RespondentPostalCode ?? PostalCodeNormalizer.Extract(response.RespondentHomeAddress),
				CountyName = response.RespondentCountyNameSnapshot,
				SubmittedByLabel = BuildSubmittedByLabel(response, userLookup),
				SubmittedUtc = response.SubmittedUtc
			})
			.ToList();
	}

	public async Task<IReadOnlyList<EmployeeListItem>> GetEmployeesAsync(string? search = null, CancellationToken cancellationToken = default)
	{
		var query = _userManager.Users.AsQueryable();

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim().ToUpperInvariant();
			query = query.Where(user =>
				(user.FirstName != null && user.FirstName.ToUpper().Contains(term)) ||
				(user.LastName != null && user.LastName.ToUpper().Contains(term)) ||
				(user.Email != null && user.Email.ToUpper().Contains(term)) ||
				(user.UserName != null && user.UserName.ToUpper().Contains(term)));
		}

		var users = await query
			.OrderBy(user => user.LastName)
			.ThenBy(user => user.FirstName)
			.ThenBy(user => user.Email)
			.ToListAsync(cancellationToken);

		var items = new List<EmployeeListItem>(users.Count);
		foreach (var user in users)
		{
			var roles = await _userManager.GetRolesAsync(user);
			var roleName = roles.FirstOrDefault() ?? RoleNames.Employee;
			if (!string.IsNullOrWhiteSpace(search)
				&& !MatchesEmployeeSearch(user, roleName, search))
			{
				continue;
			}

			items.Add(new EmployeeListItem
			{
				Id = user.Id,
				FirstName = user.FirstName ?? string.Empty,
				LastName = user.LastName ?? string.Empty,
				Email = user.Email ?? string.Empty,
				RoleName = roleName
			});
		}

		return items;
	}

	private static bool MatchesEmployeeSearch(ApplicationUser user, string roleName, string search)
	{
		var term = search.Trim();
		if (string.IsNullOrWhiteSpace(term))
		{
			return true;
		}

		return (user.FirstName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
			|| (user.LastName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
			|| (user.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
			|| (user.UserName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
			|| roleName.Contains(term, StringComparison.OrdinalIgnoreCase);
	}

	public async Task<EmployeeEditModel> GetEmployeeAsync(string? id, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			return new EmployeeEditModel();
		}

		var user = await _userManager.Users.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
			?? throw new InvalidOperationException("The requested employee was not found.");
		var roles = await _userManager.GetRolesAsync(user);

		return new EmployeeEditModel
		{
			Id = user.Id,
			FirstName = user.FirstName ?? string.Empty,
			LastName = user.LastName ?? string.Empty,
			Email = user.Email ?? string.Empty,
			RoleName = roles.FirstOrDefault() ?? RoleNames.Employee
		};
	}

	public async Task<string> SaveEmployeeAsync(EmployeeEditModel model, CancellationToken cancellationToken = default)
	{
		if (model.RoleName != RoleNames.Admin && model.RoleName != RoleNames.Employee)
		{
			throw new InvalidOperationException("An employee must be assigned either the Admin or Employee role.");
		}

		ApplicationUser user;
		if (string.IsNullOrWhiteSpace(model.Id))
		{
			if (string.IsNullOrWhiteSpace(model.Password))
			{
				throw new InvalidOperationException("A password is required when creating a new employee.");
			}

			user = new ApplicationUser
			{
				UserName = model.Email.Trim(),
				Email = model.Email.Trim(),
				EmailConfirmed = true,
				FirstName = model.FirstName.Trim(),
				LastName = model.LastName.Trim()
			};

			var createResult = await _userManager.CreateAsync(user, model.Password);
			if (!createResult.Succeeded)
			{
				throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(static error => error.Description)));
			}
		}
		else
		{
			user = await _userManager.FindByIdAsync(model.Id)
				?? throw new InvalidOperationException("The requested employee was not found.");
			user.UserName = model.Email.Trim();
			user.Email = model.Email.Trim();
			user.FirstName = model.FirstName.Trim();
			user.LastName = model.LastName.Trim();

			var updateResult = await _userManager.UpdateAsync(user);
			if (!updateResult.Succeeded)
			{
				throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(static error => error.Description)));
			}

			if (!string.IsNullOrWhiteSpace(model.Password))
			{
				if (await _userManager.HasPasswordAsync(user))
				{
					var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
					var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, model.Password);
					if (!resetResult.Succeeded)
					{
						throw new InvalidOperationException(string.Join("; ", resetResult.Errors.Select(static error => error.Description)));
					}
				}
				else
				{
					var addPasswordResult = await _userManager.AddPasswordAsync(user, model.Password);
					if (!addPasswordResult.Succeeded)
					{
						throw new InvalidOperationException(string.Join("; ", addPasswordResult.Errors.Select(static error => error.Description)));
					}
				}
			}
		}

		await SynchronizeRolesAsync(user, model.RoleName);
		return user.Id;
	}

	public async Task<SurveySessionModel?> GetPublicSessionAsync(string token, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			return null;
		}

		var assignment = await QueryAssignmentsForSession()
			.AsNoTracking()
			.FirstOrDefaultAsync(entity => entity.PublicToken == token.Trim(), cancellationToken);

		return assignment is null ? null : await BuildSessionModelAsync(assignment, false, cancellationToken);
	}

	public async Task<SurveySessionModel?> GetStaffSessionAsync(int assignmentId, CancellationToken cancellationToken = default)
	{
		var assignment = await QueryAssignmentsForSession()
			.AsNoTracking()
			.FirstOrDefaultAsync(entity => entity.Id == assignmentId, cancellationToken);

		return assignment is null ? null : await BuildSessionModelAsync(assignment, true, cancellationToken);
	}

	public async Task<SubmitSurveyResult> SubmitAsync(SurveySubmissionModel model, string? submittedByUserId, CancellationToken cancellationToken = default)
	{
		var assignment = await QueryAssignmentsForSession()
			.FirstOrDefaultAsync(entity => entity.Id == model.AssignmentId && entity.PublicToken == model.Token, cancellationToken);
		if (assignment is null)
		{
			return new SubmitSurveyResult
			{
				Succeeded = false,
				Message = "The survey link is invalid."
			};
		}

		if (assignment.Response is not null)
		{
			return new SubmitSurveyResult
			{
				Succeeded = false,
				Message = "This survey has already been submitted."
			};
		}

		if (assignment.IsExpired(DateTimeOffset.UtcNow))
		{
			return new SubmitSurveyResult
			{
				Succeeded = false,
				Message = "This survey link has expired."
			};
		}

		var resolvedAddress = await ResolveOrCreatePostalAddressAsync(
			model.Contact.CountryId,
			model.Contact.StateProvinceId,
			model.Contact.CountyId,
			model.Contact.AddressLine1,
			model.Contact.AddressLine2,
			model.Contact.City,
			model.Contact.PostalCode,
			cancellationToken);

		var response = new SurveyResponse(
			assignment.Id,
			submittedByUserId,
			model.IsStaffMode,
			model.Contact.FirstName,
			model.Contact.MiddleName,
			model.Contact.LastName,
			resolvedAddress.PostalAddress.Id,
			model.Contact.AddressLine1,
			model.Contact.AddressLine2,
			model.Contact.City,
			resolvedAddress.StateProvince.Code,
			model.Contact.PostalCode,
			resolvedAddress.County?.FipsCode,
			resolvedAddress.County?.Name,
			resolvedAddress.StateProvince.Code,
			model.Contact.PhoneNumber,
			model.Contact.BestTimeToContact,
			model.Contact.Email,
			assignment.SurveyVersion.SurveyDefinition.Name,
			assignment.SurveyVersion.DisplayName,
			resolvedAddress.Country.Name);

		var answersByQuestionId = model.Answers
			.GroupBy(answer => answer.QuestionId)
			.ToDictionary(group => group.Key, group => group.Last());

		foreach (var question in assignment.SurveyVersion.Sections
			.OrderBy(section => section.SortOrder)
			.SelectMany(section => section.Questions.OrderBy(question => question.SortOrder)))
		{
			answersByQuestionId.TryGetValue(question.Id, out var answerInput);
			var answer = CreateAnswer(question, answerInput);
			if (answer is not null)
			{
				response.Answers.Add(answer);
			}
		}

		_dbContext.SurveyResponses.Add(response);
		await _dbContext.SaveChangesAsync(cancellationToken);

		return new SubmitSurveyResult
		{
			Succeeded = true,
			Message = "Your survey has been submitted.",
			ResponseId = response.Id
		};
	}

	private IQueryable<SurveyAssignment> QueryAssignmentsForSession()
	{
		return _dbContext.SurveyAssignments
			.Include(assignment => assignment.Person)
				.ThenInclude(person => person.PostalAddress)
			.Include(assignment => assignment.Response)
			.Include(assignment => assignment.SurveyVersion)
				.ThenInclude(version => version.SurveyDefinition)
			.Include(assignment => assignment.SurveyVersion)
				.ThenInclude(version => version.Sections)
					.ThenInclude(section => section.Questions)
						.ThenInclude(question => question.Options);
	}

	private async Task<SurveySessionModel> BuildSessionModelAsync(SurveyAssignment assignment, bool isStaffMode, CancellationToken cancellationToken)
	{
		return new SurveySessionModel
		{
			AssignmentId = assignment.Id,
			Token = assignment.PublicToken,
			SurveyName = assignment.SurveyVersion.SurveyDefinition.Name,
			VersionName = assignment.SurveyVersion.DisplayName,
			IsStaffMode = isStaffMode,
			IsExpired = assignment.IsExpired(DateTimeOffset.UtcNow),
			IsCompleted = assignment.Response is not null,
			Contact = await BuildRespondentContactModelAsync(assignment.Person, cancellationToken),
			Sections = assignment.SurveyVersion.Sections
				.OrderBy(section => section.SortOrder)
				.Select(section => new SurveySectionStepModel
				{
					Id = section.Id,
					Title = section.Title,
					Description = section.Description,
					SortOrder = section.SortOrder,
					Questions = section.Questions
						.OrderBy(question => question.SortOrder)
						.Select(question => new SurveyQuestionStepModel
						{
							Id = question.Id,
							Prompt = question.Prompt,
							HelpText = question.HelpText,
							Type = question.Type,
							IsRequired = question.IsRequired,
							SortOrder = question.SortOrder,
							Options = question.Options
								.OrderBy(option => option.SortOrder)
								.Select(option => new SelectOption
								{
									Value = option.Id.ToString(),
									Label = option.Label
								})
								.ToList()
						})
						.ToList()
				})
				.ToList()
		};
	}

	private SurveyAnswer? CreateAnswer(SurveyQuestion question, SurveyAnswerInputModel? input)
	{
		return question.Type switch
		{
			SurveyQuestionType.YesNo => CreateYesNoAnswer(question, input),
			SurveyQuestionType.SingleChoice => CreateSingleChoiceAnswer(question, input),
			SurveyQuestionType.MultiSelect => CreateMultiSelectAnswer(question, input),
			SurveyQuestionType.LongText => CreateLongTextAnswer(question, input),
			_ => throw new InvalidOperationException("Unsupported question type.")
		};
	}

	private SurveyAnswer? CreateYesNoAnswer(SurveyQuestion question, SurveyAnswerInputModel? input)
	{
		if (input?.YesNoAnswer is null)
		{
			if (question.IsRequired)
			{
				throw new InvalidOperationException($"A response is required for '{question.Prompt}'.");
			}

			return null;
		}

		return new SurveyAnswer(question.Id, question.Prompt, question.Type, null, input.YesNoAnswer, null, null);
	}

	private SurveyAnswer? CreateSingleChoiceAnswer(SurveyQuestion question, SurveyAnswerInputModel? input)
	{
		if (input?.SelectedOptionId is null)
		{
			if (question.IsRequired)
			{
				throw new InvalidOperationException($"A selection is required for '{question.Prompt}'.");
			}

			return null;
		}

		if (!question.Options.Any(option => option.Id == input.SelectedOptionId.Value))
		{
			throw new InvalidOperationException($"The submitted answer for '{question.Prompt}' is invalid.");
		}

		return new SurveyAnswer(question.Id, question.Prompt, question.Type, null, null, input.SelectedOptionId, null);
	}

	private SurveyAnswer? CreateMultiSelectAnswer(SurveyQuestion question, SurveyAnswerInputModel? input)
	{
		var selectedIds = (input?.SelectedOptionIds ?? [])
			.Where(selectedId => selectedId > 0)
			.Distinct()
			.ToList();

		if (selectedIds.Count == 0)
		{
			if (question.IsRequired)
			{
				throw new InvalidOperationException($"At least one selection is required for '{question.Prompt}'.");
			}

			return null;
		}

		if (selectedIds.Any(selectedId => question.Options.All(option => option.Id != selectedId)))
		{
			throw new InvalidOperationException($"The submitted answer for '{question.Prompt}' is invalid.");
		}

		return new SurveyAnswer(
			question.Id,
			question.Prompt,
			question.Type,
			null,
			null,
			null,
			JsonSerializer.Serialize(selectedIds, JsonOptions));
	}

	private SurveyAnswer? CreateLongTextAnswer(SurveyQuestion question, SurveyAnswerInputModel? input)
	{
		var text = input?.TextAnswer?.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			if (question.IsRequired)
			{
				throw new InvalidOperationException($"A response is required for '{question.Prompt}'.");
			}

			return null;
		}

		return new SurveyAnswer(question.Id, question.Prompt, question.Type, text, null, null, null);
	}

	private async Task<IReadOnlyList<SelectOption>> GetSurveyDefinitionOptionsAsync(int? includeSurveyDefinitionId, CancellationToken cancellationToken)
	{
		return await _dbContext.SurveyDefinitions
			.AsNoTracking()
			.Where(definition => !definition.IsArchived || (includeSurveyDefinitionId.HasValue && definition.Id == includeSurveyDefinitionId.Value))
			.OrderBy(definition => definition.Name)
			.Select(definition => new SelectOption
			{
				Value = definition.Id.ToString(),
				Label = definition.Name
			})
			.ToListAsync(cancellationToken);
	}

	private async Task<IReadOnlyList<SelectOption>> GetSurveyVersionOptionsAsync(
		bool includeUnpublished,
		int? includeVersionId,
		CancellationToken cancellationToken)
	{
		return await _dbContext.SurveyVersions
			.AsNoTracking()
			.Include(version => version.SurveyDefinition)
			.Where(version =>
				(!version.IsArchived && !version.SurveyDefinition.IsArchived) ||
				(includeVersionId.HasValue && version.Id == includeVersionId.Value))
			.Where(version => includeUnpublished || version.IsPublished || (includeVersionId.HasValue && version.Id == includeVersionId.Value))
			.OrderBy(version => version.SurveyDefinition.Name)
			.ThenBy(version => version.VersionNumber)
			.Select(version => new SelectOption
			{
				Value = version.Id.ToString(),
				Label = $"{version.SurveyDefinition.Name} - {version.DisplayName}"
			})
			.ToListAsync(cancellationToken);
	}

	private async Task<IReadOnlyList<SelectOption>> GetSurveySectionOptionsAsync(int? includeSectionId, CancellationToken cancellationToken)
	{
		return await _dbContext.SurveySections
			.AsNoTracking()
			.Include(section => section.SurveyVersion)
				.ThenInclude(version => version.SurveyDefinition)
			.Where(section =>
				(!section.SurveyVersion.IsArchived && !section.SurveyVersion.SurveyDefinition.IsArchived) ||
				(includeSectionId.HasValue && section.Id == includeSectionId.Value))
			.OrderBy(section => section.SurveyVersion.SurveyDefinition.Name)
			.ThenBy(section => section.SurveyVersion.VersionNumber)
			.ThenBy(section => section.SortOrder)
			.Select(section => new SelectOption
			{
				Value = section.Id.ToString(),
				Label = $"{section.SurveyVersion.SurveyDefinition.Name} - {section.SurveyVersion.DisplayName} - {section.Title}"
			})
			.ToListAsync(cancellationToken);
	}

	private async Task<IReadOnlyList<SelectOption>> GetQuestionSelectOptionsAsync(int? includeQuestionId, CancellationToken cancellationToken)
	{
		return await _dbContext.SurveyQuestions
			.AsNoTracking()
			.Include(question => question.SurveySection)
				.ThenInclude(section => section.SurveyVersion)
					.ThenInclude(version => version.SurveyDefinition)
			.Where(question => question.Type == SurveyQuestionType.SingleChoice || question.Type == SurveyQuestionType.MultiSelect)
			.Where(question =>
				(!question.SurveySection.SurveyVersion.IsArchived && !question.SurveySection.SurveyVersion.SurveyDefinition.IsArchived) ||
				(includeQuestionId.HasValue && question.Id == includeQuestionId.Value))
			.OrderBy(question => question.SurveySection.SurveyVersion.SurveyDefinition.Name)
			.ThenBy(question => question.SurveySection.SurveyVersion.VersionNumber)
			.ThenBy(question => question.SurveySection.SortOrder)
			.ThenBy(question => question.SortOrder)
			.Select(question => new SelectOption
			{
				Value = question.Id.ToString(),
				Label = $"{question.SurveySection.SurveyVersion.SurveyDefinition.Name} - {question.SurveySection.Title} - {TrimLabel(question.Prompt, 80)}"
			})
			.ToListAsync(cancellationToken);
	}

	private async Task<IReadOnlyList<SelectOption>> GetPersonSelectOptionsAsync(CancellationToken cancellationToken)
	{
		return await _dbContext.People
			.AsNoTracking()
			.OrderBy(person => person.LastName)
			.ThenBy(person => person.FirstName)
			.Select(person => new SelectOption
			{
				Value = person.Id.ToString(),
				Label = $"{person.LastName}, {person.FirstName} ({person.Email})"
			})
			.ToListAsync(cancellationToken);
	}

	private async Task<int> GetNextVersionNumberAsync(int surveyDefinitionId, CancellationToken cancellationToken)
	{
		var maxVersion = await _dbContext.SurveyVersions
			.AsNoTracking()
			.Where(version => version.SurveyDefinitionId == surveyDefinitionId)
			.Select(version => (int?)version.VersionNumber)
			.MaxAsync(cancellationToken);

		return (maxVersion ?? 0) + 1;
	}

	private async Task EnsureSurveyDefinitionExistsAsync(int surveyDefinitionId, CancellationToken cancellationToken)
	{
		var exists = await _dbContext.SurveyDefinitions.AnyAsync(definition => definition.Id == surveyDefinitionId, cancellationToken);
		if (!exists)
		{
			throw new InvalidOperationException("The selected survey was not found.");
		}
	}

	private async Task EnsurePersonExistsAsync(int personId, CancellationToken cancellationToken)
	{
		var exists = await _dbContext.People.AnyAsync(person => person.Id == personId, cancellationToken);
		if (!exists)
		{
			throw new InvalidOperationException("The selected person was not found.");
		}
	}

	private async Task EnsureVersionEditableAsync(int surveyVersionId, CancellationToken cancellationToken)
	{
		var hasAssignments = await _dbContext.SurveyAssignments.AnyAsync(assignment => assignment.SurveyVersionId == surveyVersionId, cancellationToken);
		if (hasAssignments)
		{
			throw new InvalidOperationException("This survey version is locked because it has already been assigned.");
		}
	}

	private async Task<int> GetVersionIdForSectionAsync(int sectionId, CancellationToken cancellationToken)
	{
		var versionId = await _dbContext.SurveySections
			.Where(section => section.Id == sectionId)
			.Select(section => section.SurveyVersionId)
			.FirstOrDefaultAsync(cancellationToken);

		return versionId == 0
			? throw new InvalidOperationException("The requested section was not found.")
			: versionId;
	}

	private async Task<int> GetVersionIdForQuestionAsync(int questionId, CancellationToken cancellationToken)
	{
		var versionId = await _dbContext.SurveyQuestions
			.Where(question => question.Id == questionId)
			.Select(question => question.SurveySection.SurveyVersionId)
			.FirstOrDefaultAsync(cancellationToken);

		return versionId == 0
			? throw new InvalidOperationException("The requested question was not found.")
			: versionId;
	}

	private async Task SynchronizeRolesAsync(ApplicationUser user, string desiredRole)
	{
		var existingRoles = await _userManager.GetRolesAsync(user);
		if (existingRoles.Contains(desiredRole, StringComparer.OrdinalIgnoreCase) && existingRoles.Count == 1)
		{
			return;
		}

		if (existingRoles.Count > 0)
		{
			var removeResult = await _userManager.RemoveFromRolesAsync(user, existingRoles);
			if (!removeResult.Succeeded)
			{
				throw new InvalidOperationException(string.Join("; ", removeResult.Errors.Select(static error => error.Description)));
			}
		}

		var addResult = await _userManager.AddToRoleAsync(user, desiredRole);
		if (!addResult.Succeeded)
		{
			throw new InvalidOperationException(string.Join("; ", addResult.Errors.Select(static error => error.Description)));
		}
	}

	private async Task<Dictionary<string, string>> GetUserDisplayLookupAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken)
	{
		if (ids.Count == 0)
		{
			return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}

		return await _userManager.Users
			.AsNoTracking()
			.Where(user => ids.Contains(user.Id))
			.ToDictionaryAsync(
				user => user.Id,
				user => user.DisplayName,
				StringComparer.OrdinalIgnoreCase,
				cancellationToken);
	}

	private static string BuildSubmittedByLabel(SurveyResponse response, IReadOnlyDictionary<string, string> userLookup)
	{
		if (response.SubmittedByEmployee)
		{
			if (!string.IsNullOrWhiteSpace(response.SubmittedByUserId) && userLookup.TryGetValue(response.SubmittedByUserId, out var employeeName))
			{
				return $"{employeeName} (employee)";
			}

			return "Employee";
		}

		return "Self-service";
	}

	private static string FormatAnswer(SurveyAnswer answer)
	{
		return answer.QuestionType switch
		{
			SurveyQuestionType.YesNo => answer.YesNoValue == true ? "Yes" : "No",
			SurveyQuestionType.SingleChoice => FormatSingleChoice(answer),
			SurveyQuestionType.MultiSelect => FormatMultiSelect(answer),
			SurveyQuestionType.LongText => answer.AnswerText ?? string.Empty,
			_ => string.Empty
		};
	}

	private static string FormatSingleChoice(SurveyAnswer answer)
	{
		if (!answer.SelectedOptionId.HasValue)
		{
			return string.Empty;
		}

		return answer.SurveyQuestion.Options
			.FirstOrDefault(option => option.Id == answer.SelectedOptionId.Value)?.Label
			?? answer.SelectedOptionId.Value.ToString();
	}

	private static string FormatMultiSelect(SurveyAnswer answer)
	{
		if (string.IsNullOrWhiteSpace(answer.SelectedOptionIdsJson))
		{
			return string.Empty;
		}

		try
		{
			var ids = JsonSerializer.Deserialize<List<int>>(answer.SelectedOptionIdsJson, JsonOptions) ?? [];
			var labels = ids
				.Select(selectedId => answer.SurveyQuestion.Options.FirstOrDefault(option => option.Id == selectedId)?.Label ?? selectedId.ToString());
			return string.Join(", ", labels);
		}
		catch
		{
			return answer.SelectedOptionIdsJson;
		}
	}

	private static bool SupportsOptions(SurveyQuestionType questionType)
	{
		return questionType is SurveyQuestionType.SingleChoice or SurveyQuestionType.MultiSelect;
	}

	private static int TryParseInt(string? value)
	{
		return int.TryParse(value, out var parsed) ? parsed : 0;
	}

	private static string GeneratePublicToken()
	{
		return Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
	}

	private static string BuildFullName(string firstName, string? middleName, string lastName)
	{
		return string.Join(" ", new[] { firstName, middleName, lastName }.Where(static part => !string.IsNullOrWhiteSpace(part)));
	}

	private static string TrimLabel(string value, int maxLength)
	{
		var trimmed = value.Trim();
		return trimmed.Length <= maxLength ? trimmed : $"{trimmed[..(maxLength - 3)]}...";
	}

	private static string FormatAddressOrFallback(
		string? addressLine1,
		string? addressLine2,
		string? city,
		string? state,
		string? postalCode,
		string fallback)
	{
		if (!string.IsNullOrWhiteSpace(addressLine1)
			&& !string.IsNullOrWhiteSpace(city)
			&& !string.IsNullOrWhiteSpace(state)
			&& !string.IsNullOrWhiteSpace(postalCode))
		{
			return AddressFormatter.Format(addressLine1, addressLine2, city, state, postalCode);
		}

		return fallback;
	}
}
