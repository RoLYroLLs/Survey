using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure.Identity;
using Survey.Infrastructure.Persistence;
using Survey.Infrastructure.Security;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService(
	SurveyDbContext dbContext,
	UserManager<ApplicationUser> userManager,
	ITenantContextAccessor tenantContextAccessor,
	ITenantPermissionEvaluator tenantPermissionEvaluator,
	IPlatformPermissionEvaluator platformPermissionEvaluator,
	IAuditWriter auditWriter,
	TenantExecutionContext tenantExecutionContext) : ITenantAdministrationService, IPlatformAdministrationService, ISurveyExperienceService
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	private readonly SurveyDbContext _dbContext = dbContext;
	private readonly UserManager<ApplicationUser> _userManager = userManager;
	private readonly ITenantContextAccessor _tenantContextAccessor = tenantContextAccessor;
	private readonly ITenantPermissionEvaluator _tenantPermissionEvaluator = tenantPermissionEvaluator;
	private readonly IPlatformPermissionEvaluator _platformPermissionEvaluator = platformPermissionEvaluator;
	private readonly IAuditWriter _auditWriter = auditWriter;
	private readonly TenantExecutionContext _tenantExecutionContext = tenantExecutionContext;

	public async Task<IReadOnlyList<SurveyDefinitionListItem>> GetSurveyDefinitionsAsync(bool archivedOnly = false, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.SurveysView, cancellationToken);

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
		await RequireTenantPermissionAsync(TenantPermissionKeys.SurveysView, cancellationToken);

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
		await RequireTenantPermissionAsync(model.Id.HasValue ? TenantPermissionKeys.SurveysEdit : TenantPermissionKeys.SurveysCreate, cancellationToken);
		var isNew = !model.Id.HasValue;

		SurveyDefinition entity;
		if (!isNew)
		{
			var surveyDefinitionId = model.Id ?? throw new InvalidOperationException("The requested survey was not found.");
			entity = await _dbContext.SurveyDefinitions.FirstOrDefaultAsync(definition => definition.Id == surveyDefinitionId, cancellationToken)
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
		await AuditTenantEntityChangeAsync(
			isNew ? "tenant.survey.created" : "tenant.survey.updated",
			nameof(SurveyDefinition),
			entity.Id,
			$"Survey '{entity.Name}' was {(isNew ? "created" : "saved")} (archived: {entity.IsArchived}).",
			cancellationToken);
		return entity.Id;
	}

	public async Task<IReadOnlyList<SurveyVersionListItem>> GetSurveyVersionsAsync(int? surveyDefinitionId, bool archivedOnly = false, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.SurveysView, cancellationToken);

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
		await RequireTenantPermissionAsync(TenantPermissionKeys.SurveysView, cancellationToken);

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
		await RequireTenantPermissionAsync(model.Id.HasValue ? TenantPermissionKeys.SurveysEdit : TenantPermissionKeys.SurveysCreate, cancellationToken);
		var isNew = !model.Id.HasValue;

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
		await AuditTenantEntityChangeAsync(
			isNew ? "tenant.survey-version.created" : "tenant.survey-version.updated",
			nameof(SurveyVersion),
			entity.Id,
			$"Survey version '{entity.DisplayName}' was {(isNew ? "created" : "saved")} (archived: {entity.IsArchived}, published: {entity.IsPublished}).",
			cancellationToken);
		return entity.Id;
	}

	public async Task<int> CloneSurveyVersionAsync(int surveyVersionId, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.SurveysCreate, cancellationToken);

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

		await AuditTenantEntityChangeAsync(
			"tenant.survey-version.cloned",
			nameof(SurveyVersion),
			clone.Id,
			$"Survey version '{source.DisplayName}' was cloned into '{clone.DisplayName}'.",
			cancellationToken);
		return clone.Id;
	}

	public async Task<IReadOnlyList<SurveySectionListItem>> GetSurveySectionsAsync(int surveyVersionId, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.SurveysView, cancellationToken);

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
		await RequireTenantPermissionAsync(TenantPermissionKeys.SurveysView, cancellationToken);

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
		await RequireTenantPermissionAsync(model.Id.HasValue ? TenantPermissionKeys.SurveysEdit : TenantPermissionKeys.SurveysCreate, cancellationToken);
		var isNew = !model.Id.HasValue;

		if (!isNew)
		{
			var sectionId = model.Id ?? throw new InvalidOperationException("The requested section was not found.");
			var section = await _dbContext.SurveySections.FirstOrDefaultAsync(entity => entity.Id == sectionId, cancellationToken)
				?? throw new InvalidOperationException("The requested section was not found.");
			await EnsureVersionEditableAsync(section.SurveyVersionId, cancellationToken);
			section.Update(model.Title, model.Description, model.SortOrder);
			await _dbContext.SaveChangesAsync(cancellationToken);
			await AuditTenantEntityChangeAsync("tenant.survey-section.updated", nameof(SurveySection), section.Id, $"Survey section '{section.Title}' was updated.", cancellationToken);
			return section.Id;
		}

		await EnsureVersionEditableAsync(model.SurveyVersionId, cancellationToken);
		var entity = new SurveySection(model.SurveyVersionId, model.Title, model.Description, model.SortOrder);
		_dbContext.SurveySections.Add(entity);
		await _dbContext.SaveChangesAsync(cancellationToken);
		await AuditTenantEntityChangeAsync("tenant.survey-section.created", nameof(SurveySection), entity.Id, $"Survey section '{entity.Title}' was created.", cancellationToken);
		return entity.Id;
	}

	public async Task<IReadOnlyList<SurveyQuestionListItem>> GetSurveyQuestionsAsync(int surveySectionId, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.SurveysView, cancellationToken);

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
		await RequireTenantPermissionAsync(TenantPermissionKeys.SurveysView, cancellationToken);

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
		await RequireTenantPermissionAsync(model.Id.HasValue ? TenantPermissionKeys.SurveysEdit : TenantPermissionKeys.SurveysCreate, cancellationToken);
		var isNew = !model.Id.HasValue;

		if (!isNew)
		{
			var questionId = model.Id ?? throw new InvalidOperationException("The requested question was not found.");
			var question = await _dbContext.SurveyQuestions.FirstOrDefaultAsync(entity => entity.Id == questionId, cancellationToken)
				?? throw new InvalidOperationException("The requested question was not found.");
			var versionId = await GetVersionIdForQuestionAsync(question.Id, cancellationToken);
			await EnsureVersionEditableAsync(versionId, cancellationToken);
			question.Update(model.Prompt, model.HelpText, model.Type, model.IsRequired, model.SortOrder);
			await _dbContext.SaveChangesAsync(cancellationToken);
			await AuditTenantEntityChangeAsync("tenant.survey-question.updated", nameof(SurveyQuestion), question.Id, $"Survey question '{TrimLabel(question.Prompt, 80)}' was updated.", cancellationToken);
			return question.Id;
		}

		var parentVersionId = await GetVersionIdForSectionAsync(model.SurveySectionId, cancellationToken);
		await EnsureVersionEditableAsync(parentVersionId, cancellationToken);
		var entity = new SurveyQuestion(model.SurveySectionId, model.Prompt, model.HelpText, model.Type, model.IsRequired, model.SortOrder);
		_dbContext.SurveyQuestions.Add(entity);
		await _dbContext.SaveChangesAsync(cancellationToken);
		await AuditTenantEntityChangeAsync("tenant.survey-question.created", nameof(SurveyQuestion), entity.Id, $"Survey question '{TrimLabel(entity.Prompt, 80)}' was created.", cancellationToken);
		return entity.Id;
	}

	public async Task<IReadOnlyList<QuestionOptionListItem>> GetQuestionOptionsAsync(int surveyQuestionId, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.SurveysView, cancellationToken);

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
		await RequireTenantPermissionAsync(TenantPermissionKeys.SurveysView, cancellationToken);

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
		await RequireTenantPermissionAsync(model.Id.HasValue ? TenantPermissionKeys.SurveysEdit : TenantPermissionKeys.SurveysCreate, cancellationToken);
		var isNew = !model.Id.HasValue;

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

		if (!isNew)
		{
			var optionId = model.Id ?? throw new InvalidOperationException("The requested option was not found.");
			var option = await _dbContext.QuestionOptions.FirstOrDefaultAsync(entity => entity.Id == optionId, cancellationToken)
				?? throw new InvalidOperationException("The requested option was not found.");
			option.Update(model.Label, model.SortOrder);
			await _dbContext.SaveChangesAsync(cancellationToken);
			await AuditTenantEntityChangeAsync("tenant.question-option.updated", nameof(QuestionOption), option.Id, $"Question option '{option.Label}' was updated.", cancellationToken);
			return option.Id;
		}

		var entity = new QuestionOption(model.SurveyQuestionId, model.Label, model.SortOrder);
		_dbContext.QuestionOptions.Add(entity);
		await _dbContext.SaveChangesAsync(cancellationToken);
		await AuditTenantEntityChangeAsync("tenant.question-option.created", nameof(QuestionOption), entity.Id, $"Question option '{entity.Label}' was created.", cancellationToken);
		return entity.Id;
	}

	public async Task<IReadOnlyList<PersonListItem>> GetPeopleAsync(bool archivedOnly = false, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.PeopleView, cancellationToken);

		return await _dbContext.People
			.AsNoTracking()
			.Where(person => person.IsArchived == archivedOnly)
			.Include(person => person.Locations)
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
				PhoneNumber = person.PhoneNumber,
				LocationCount = person.Locations.Count,
				IsArchived = person.IsArchived
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<PersonEditModel> GetPersonAsync(int? id, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.PeopleView, cancellationToken);

		if (!id.HasValue)
		{
			return await BuildPersonEditModelAsync(null, cancellationToken);
		}

		var entity = await _dbContext.People
			.AsNoTracking()
			.Include(person => person.PostalAddress)
			.Include(person => person.MailingPostalAddress)
			.Include(person => person.Phones)
			.Include(person => person.Emails)
			.Include(person => person.Locations)
			.FirstOrDefaultAsync(person => person.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested person was not found.");

		return await BuildPersonEditModelAsync(entity, cancellationToken);
	}

	public async Task SetPersonArchivedAsync(int id, bool isArchived, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.PeopleEdit, cancellationToken);

		var entity = await _dbContext.People.FirstOrDefaultAsync(person => person.Id == id, cancellationToken)
			?? throw new InvalidOperationException("The requested person was not found.");

		entity.SetArchived(isArchived);
		await _dbContext.SaveChangesAsync(cancellationToken);
		await AuditTenantEntityChangeAsync(
			isArchived ? "tenant.person.archived" : "tenant.person.restored",
			nameof(Person),
			entity.Id,
			$"Person '{BuildFullName(entity.FirstName, entity.MiddleName, entity.LastName)}' was {(isArchived ? "archived" : "restored")}.",
			cancellationToken);
	}

	public async Task<int> SavePersonAsync(PersonEditModel model, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(model.Id.HasValue ? TenantPermissionKeys.PeopleEdit : TenantPermissionKeys.PeopleCreate, cancellationToken);
		var isNew = !model.Id.HasValue;

		var normalizedPhones = NormalizePhoneContacts(model.Phones);
		var normalizedEmails = NormalizeEmailContacts(model.Emails);
		var primaryPhone = normalizedPhones.FirstOrDefault();
		var primaryEmail = normalizedEmails.FirstOrDefault();
		var normalizedBestTimeToContact = ContactOptionCatalog.NormalizeBestTime(model.BestTimeToContact);
		var normalizedPreferredContactMethod = ContactOptionCatalog.NormalizePreferredContactMethod(model.PreferredContactMethod);
		var mailingAddressInput = model.MailingAddress;

		var resolvedPhysicalAddress = await ResolveOrCreatePostalAddressAsync(
			model.PhysicalAddress.CountryId,
			model.PhysicalAddress.StateProvinceId,
			model.PhysicalAddress.CountyId,
			model.PhysicalAddress.AddressLine1,
			model.PhysicalAddress.AddressLine2,
			model.PhysicalAddress.City,
			model.PhysicalAddress.PostalCode,
			cancellationToken);
		var resolvedMailingAddress = await ResolveOrCreatePostalAddressAsync(
			mailingAddressInput.CountryId,
			mailingAddressInput.StateProvinceId,
			mailingAddressInput.CountyId,
			mailingAddressInput.AddressLine1,
			mailingAddressInput.AddressLine2,
			mailingAddressInput.City,
			mailingAddressInput.PostalCode,
			cancellationToken);

		Person entity;
		if (model.Id.HasValue)
		{
			entity = await _dbContext.People
				.Include(person => person.Phones)
				.Include(person => person.Emails)
				.FirstOrDefaultAsync(person => person.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested person was not found.");
			entity.Update(
				model.FirstName,
				model.MiddleName,
				model.LastName,
				resolvedPhysicalAddress.PostalAddress.Id,
				model.PhysicalAddress.AddressLine1,
				model.PhysicalAddress.AddressLine2,
				model.PhysicalAddress.City,
				resolvedPhysicalAddress.StateProvince.Code,
				model.PhysicalAddress.PostalCode,
				resolvedMailingAddress.PostalAddress.Id,
				mailingAddressInput.AddressLine1,
				mailingAddressInput.AddressLine2,
				mailingAddressInput.City,
				resolvedMailingAddress.StateProvince.Code,
				mailingAddressInput.PostalCode,
				primaryPhone?.PhoneNumber,
				normalizedBestTimeToContact,
				normalizedPreferredContactMethod,
				primaryEmail?.EmailAddress,
				resolvedPhysicalAddress.Country.Name,
				resolvedMailingAddress.Country.Name);
		}
		else
		{
			entity = new Person(
				model.FirstName,
				model.MiddleName,
				model.LastName,
				resolvedPhysicalAddress.PostalAddress.Id,
				model.PhysicalAddress.AddressLine1,
				model.PhysicalAddress.AddressLine2,
				model.PhysicalAddress.City,
				resolvedPhysicalAddress.StateProvince.Code,
				model.PhysicalAddress.PostalCode,
				resolvedMailingAddress.PostalAddress.Id,
				mailingAddressInput.AddressLine1,
				mailingAddressInput.AddressLine2,
				mailingAddressInput.City,
				resolvedMailingAddress.StateProvince.Code,
				mailingAddressInput.PostalCode,
				primaryPhone?.PhoneNumber,
				normalizedBestTimeToContact,
				normalizedPreferredContactMethod,
				primaryEmail?.EmailAddress,
				resolvedPhysicalAddress.Country.Name,
				resolvedMailingAddress.Country.Name);
			_dbContext.People.Add(entity);
			await _dbContext.SaveChangesAsync(cancellationToken);
		}

		await SyncPersonPhonesAsync(entity, normalizedPhones, cancellationToken);
		await SyncPersonEmailsAsync(entity, normalizedEmails, cancellationToken);
		entity.UpdatePrimaryContactSnapshot(primaryPhone?.PhoneNumber, primaryEmail?.EmailAddress);
		await _dbContext.SaveChangesAsync(cancellationToken);
		await AuditTenantEntityChangeAsync(
			isNew ? "tenant.person.created" : "tenant.person.updated",
			nameof(Person),
			entity.Id,
			$"Person '{BuildFullName(entity.FirstName, entity.MiddleName, entity.LastName)}' was {(isNew ? "created" : "saved")}.",
			cancellationToken);
		return entity.Id;
	}

	public async Task<IReadOnlyList<SurveyAssignmentListItem>> GetAssignmentsAsync(int? personId = null, bool archivedOnly = false, string? statusFilter = null, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.AssignmentsView, cancellationToken);

		var now = DateTimeOffset.UtcNow;

		IQueryable<SurveyAssignment> query = _dbContext.SurveyAssignments
			.AsNoTracking()
			.Include(assignment => assignment.Location)
				.ThenInclude(location => location.Person)
			.Include(assignment => assignment.SurveyVersion)
				.ThenInclude(version => version.SurveyDefinition)
			.Include(assignment => assignment.Response);

		if (personId.HasValue)
		{
			query = query.Where(assignment => assignment.Location.PersonId == personId.Value);
		}

		query = archivedOnly
			? query.Where(assignment => assignment.IsArchived)
			: query.Where(assignment => !assignment.IsArchived);

		var normalizedStatusFilter = statusFilter?.Trim().ToLowerInvariant();

		var assignments = await query
			.Select(assignment => new SurveyAssignmentListItem
			{
				Id = assignment.Id,
				PersonName = BuildFullName(assignment.Location.Person.FirstName, assignment.Location.Person.MiddleName, assignment.Location.Person.LastName),
				LocationName = assignment.Location.Nickname,
				SurveyName = assignment.SurveyVersion.SurveyDefinition.Name,
				VersionName = assignment.SurveyVersion.DisplayName,
				PublicToken = assignment.PublicToken,
				ExpiresAtUtc = assignment.ExpiresAtUtc,
				CreatedUtc = assignment.CreatedUtc,
				IsArchived = assignment.IsArchived,
				IsCompleted = assignment.Response != null,
				IsExpired = assignment.ExpiresAtUtc.HasValue && assignment.ExpiresAtUtc <= now
			})
			.ToListAsync(cancellationToken);

		var filteredAssignments = normalizedStatusFilter switch
		{
			"active" => assignments.Where(assignment => !assignment.IsCompleted && !assignment.IsExpired),
			"completed" => assignments.Where(assignment => assignment.IsCompleted),
			"expired" => assignments.Where(assignment => !assignment.IsCompleted && assignment.IsExpired),
			_ => assignments
		};

		return filteredAssignments
			.OrderByDescending(assignment => assignment.CreatedUtc)
			.ToList();
	}

	public async Task<SurveyAssignmentEditModel> GetAssignmentAsync(int? id, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.AssignmentsView, cancellationToken);

		var now = DateTimeOffset.UtcNow;
		var personOptions = await GetPersonSelectOptionsAsync(cancellationToken, id.HasValue ? await GetAssignmentPersonIdAsync(id.Value, cancellationToken) : null);

		if (!id.HasValue)
		{
			var availableVersionOptions = await GetSurveyVersionOptionsAsync(includeUnpublished: false, includeVersionId: null, cancellationToken: cancellationToken);
			var personId = personOptions.Select(option => TryParseInt(option.Value)).FirstOrDefault(value => value > 0);
			var locationOptions = await GetLocationSelectOptionsAsync(personId, null, cancellationToken);
			var locationId = locationOptions.Select(option => TryParseInt(option.Value)).FirstOrDefault(value => value > 0);
			var locationPhoneOptions = await GetLocationPhoneSelectOptionsAsync(locationId, null, cancellationToken);
			var locationEmailOptions = await GetLocationEmailSelectOptionsAsync(locationId, null, cancellationToken);
			return new SurveyAssignmentEditModel
			{
				PersonId = personId,
				LocationId = locationId,
				LocationPhoneId = GetFirstOptionId(locationPhoneOptions),
				LocationEmailId = GetFirstOptionId(locationEmailOptions),
				SurveyVersionId = availableVersionOptions.Select(option => TryParseInt(option.Value)).FirstOrDefault(value => value > 0),
				PublicToken = GeneratePublicToken(),
				IsArchived = false,
				LocationOptions = locationOptions,
				LocationPhoneOptions = locationPhoneOptions,
				LocationEmailOptions = locationEmailOptions,
				PersonOptions = personOptions,
				SurveyVersionOptions = availableVersionOptions
			};
		}

		var entity = await _dbContext.SurveyAssignments
			.AsNoTracking()
			.Include(assignment => assignment.Location)
			.Include(assignment => assignment.Response)
			.FirstOrDefaultAsync(assignment => assignment.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested assignment was not found.");
		var versionOptions = await GetSurveyVersionOptionsAsync(includeUnpublished: false, includeVersionId: entity.SurveyVersionId, cancellationToken: cancellationToken);

		return new SurveyAssignmentEditModel
		{
			Id = entity.Id,
			PersonId = entity.Location.PersonId,
			LocationId = entity.LocationId,
			LocationPhoneId = entity.LocationPhoneId,
			LocationEmailId = entity.LocationEmailId,
			SurveyVersionId = entity.SurveyVersionId,
			ExpiresAtUtc = entity.ExpiresAtUtc,
			PublicToken = entity.PublicToken,
			IsArchived = entity.IsArchived,
			IsCompleted = entity.Response is not null,
			IsExpired = entity.ExpiresAtUtc.HasValue && entity.ExpiresAtUtc.Value <= now,
			LocationOptions = await GetLocationSelectOptionsAsync(entity.Location.PersonId, entity.LocationId, cancellationToken),
			LocationPhoneOptions = await GetLocationPhoneSelectOptionsAsync(entity.LocationId, entity.LocationPhoneId, cancellationToken),
			LocationEmailOptions = await GetLocationEmailSelectOptionsAsync(entity.LocationId, entity.LocationEmailId, cancellationToken),
			PersonOptions = personOptions,
			SurveyVersionOptions = versionOptions
		};
	}

	public async Task<int> SaveAssignmentAsync(SurveyAssignmentEditModel model, string? createdByUserId, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(model.Id.HasValue ? TenantPermissionKeys.AssignmentsEdit : TenantPermissionKeys.AssignmentsCreate, cancellationToken);
		var isNew = !model.Id.HasValue;

		await EnsurePersonExistsAsync(model.PersonId, cancellationToken);
		await EnsureLocationExistsAsync(model.LocationId, cancellationToken);

		if (!model.LocationPhoneId.HasValue && !model.LocationEmailId.HasValue)
		{
			throw new InvalidOperationException("Select at least one location phone or location email.");
		}

		await EnsureLocationPhoneBelongsToLocationAsync(model.LocationId, model.LocationPhoneId, cancellationToken);
		await EnsureLocationEmailBelongsToLocationAsync(model.LocationId, model.LocationEmailId, cancellationToken);

		var locationPersonId = await _dbContext.Locations
			.AsNoTracking()
			.Where(location => location.Id == model.LocationId)
			.Select(location => location.PersonId)
			.FirstOrDefaultAsync(cancellationToken);
		if (locationPersonId != model.PersonId)
		{
			throw new InvalidOperationException("The selected location does not belong to the selected person.");
		}

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

		if (!isNew)
		{
			var assignmentId = model.Id ?? throw new InvalidOperationException("The requested assignment was not found.");
			var assignment = await _dbContext.SurveyAssignments
				.Include(entity => entity.Response)
				.FirstOrDefaultAsync(entity => entity.Id == assignmentId, cancellationToken)
				?? throw new InvalidOperationException("The requested assignment was not found.");

			if (assignment.Response is not null)
			{
				throw new InvalidOperationException("Completed assignments can no longer be edited.");
			}

			assignment.Update(assignment.PublicToken, model.ExpiresAtUtc);
			assignment.SetArchived(model.IsArchived);
			await _dbContext.SaveChangesAsync(cancellationToken);
			await AuditTenantEntityChangeAsync("tenant.assignment.updated", nameof(SurveyAssignment), assignment.Id, $"Assignment '{assignment.PublicToken}' was updated (archived: {assignment.IsArchived}).", cancellationToken);
			return assignment.Id;
		}

		var entity = new SurveyAssignment(
			model.LocationId,
			model.LocationPhoneId,
			model.LocationEmailId,
			model.SurveyVersionId,
			string.IsNullOrWhiteSpace(model.PublicToken) ? GeneratePublicToken() : model.PublicToken,
			model.ExpiresAtUtc,
			await RequireCurrentUserIdAsync(cancellationToken));
		entity.SetArchived(model.IsArchived);
		_dbContext.SurveyAssignments.Add(entity);
		await _dbContext.SaveChangesAsync(cancellationToken);
		await AuditTenantEntityChangeAsync("tenant.assignment.created", nameof(SurveyAssignment), entity.Id, $"Assignment '{entity.PublicToken}' was created (archived: {entity.IsArchived}).", cancellationToken);
		return entity.Id;
	}

	public async Task SetAssignmentArchivedAsync(int id, bool isArchived, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.AssignmentsArchive, cancellationToken);

		var assignment = await _dbContext.SurveyAssignments
			.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
			?? throw new InvalidOperationException("The requested assignment was not found.");

		assignment.SetArchived(isArchived);
		await _dbContext.SaveChangesAsync(cancellationToken);
		await AuditTenantEntityChangeAsync(
			isArchived ? "tenant.assignment.archived" : "tenant.assignment.restored",
			nameof(SurveyAssignment),
			assignment.Id,
			$"Assignment '{assignment.PublicToken}' was {(isArchived ? "archived" : "restored")}.",
			cancellationToken);
	}

	public async Task<IReadOnlyList<SurveyResponseListItem>> GetResponsesAsync(CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.ResponsesView, cancellationToken);

		var responses = await _dbContext.SurveyResponses
			.AsNoTracking()
			.Include(response => response.SurveyAssignment)
				.ThenInclude(assignment => assignment.Location)
					.ThenInclude(location => location.Person)
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
		await RequireTenantPermissionAsync(TenantPermissionKeys.ResponsesView, cancellationToken);

		var normalizedFilter = postalCode?.Trim() ?? string.Empty;
		var responses = await _dbContext.SurveyResponses
			.AsNoTracking()
			.Include(response => response.SurveyAssignment)
				.ThenInclude(assignment => assignment.Location)
					.ThenInclude(location => location.Person)
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
		await RequireTenantPermissionAsync(TenantPermissionKeys.ResponsesView, cancellationToken);

		var response = await _dbContext.SurveyResponses
			.AsNoTracking()
			.Include(entity => entity.SurveyAssignment)
				.ThenInclude(assignment => assignment.Location)
					.ThenInclude(location => location.Person)
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
			PersonId = response.SurveyAssignment.Location.PersonId,
			LocationId = response.SurveyAssignment.LocationId,
			RespondentPostalAddressId = response.RespondentPostalAddressId,
			RespondentMailingPostalAddressId = response.RespondentMailingPostalAddressId,
			PersonName = BuildFullName(response.SurveyAssignment.Location.Person.FirstName, response.SurveyAssignment.Location.Person.MiddleName, response.SurveyAssignment.Location.Person.LastName),
			LocationName = response.SurveyAssignment.Location.Nickname,
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
			MailingAddress = FormatAddressOrFallback(
				response.RespondentMailingAddressLine1,
				response.RespondentMailingAddressLine2,
				response.RespondentMailingCity,
				response.RespondentMailingState,
				response.RespondentMailingPostalCode,
				response.RespondentMailingAddress),
			PostalCode = response.RespondentPostalCode ?? PostalCodeNormalizer.Extract(response.RespondentHomeAddress),
			CountyName = response.RespondentCountyNameSnapshot,
			PhoneNumber = response.RespondentPhoneNumber,
			PhoneLabel = response.RespondentPhoneLabel,
			BestTimeToContact = response.RespondentBestTimeToContact,
			PreferredContactMethod = response.RespondentPreferredContactMethod,
			Email = response.RespondentEmail,
			EmailLabel = response.RespondentEmailLabel,
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
				PersonId = response.SurveyAssignment.Location.PersonId,
				LocationId = response.SurveyAssignment.LocationId,
				PersonName = BuildFullName(response.SurveyAssignment.Location.Person.FirstName, response.SurveyAssignment.Location.Person.MiddleName, response.SurveyAssignment.Location.Person.LastName),
				LocationName = response.SurveyAssignment.Location.Nickname,
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

	public async Task<SurveySessionModel?> GetPublicSessionAsync(string token, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			return null;
		}

		var assignment = await QueryAssignmentsForSession()
			.AsNoTracking()
			.FirstOrDefaultAsync(entity => entity.PublicToken == token.Trim() && !entity.IsArchived, cancellationToken);

		if (assignment is not null)
		{
			_tenantExecutionContext.UseTenant(assignment.TenantId);
		}

		return assignment is null ? null : await BuildSessionModelAsync(assignment, false, cancellationToken);
	}

	public async Task<SurveySessionModel?> GetStaffSessionAsync(int assignmentId, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.AssignmentsFill, cancellationToken);
		var tenantId = await RequireTenantIdAsync(cancellationToken);

		var assignment = await QueryAssignmentsForSession()
			.AsNoTracking()
			.FirstOrDefaultAsync(entity => entity.Id == assignmentId && !entity.IsArchived && entity.TenantId == tenantId, cancellationToken);

		return assignment is null ? null : await BuildSessionModelAsync(assignment, true, cancellationToken);
	}

	public async Task<SubmitSurveyResult> SubmitAsync(SurveySubmissionModel model, string? submittedByUserId, CancellationToken cancellationToken = default)
	{
		if (model.IsStaffMode)
		{
			await RequireTenantPermissionAsync(TenantPermissionKeys.AssignmentsFill, cancellationToken);
		}

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

		if (model.IsStaffMode)
		{
			var tenantId = await RequireTenantIdAsync(cancellationToken);
			if (assignment.TenantId != tenantId)
			{
				return new SubmitSurveyResult
				{
					Succeeded = false,
					Message = "The survey link is invalid."
				};
			}
		}

		_tenantExecutionContext.UseTenant(assignment.TenantId);

		if (assignment.IsArchived)
		{
			return new SubmitSurveyResult
			{
				Succeeded = false,
				Message = "This survey assignment is archived."
			};
		}

		var resolvedPhysicalAddress = await ResolveOrCreatePostalAddressAsync(
			model.Contact.PhysicalAddress.CountryId,
			model.Contact.PhysicalAddress.StateProvinceId,
			model.Contact.PhysicalAddress.CountyId,
			model.Contact.PhysicalAddress.AddressLine1,
			model.Contact.PhysicalAddress.AddressLine2,
			model.Contact.PhysicalAddress.City,
			model.Contact.PhysicalAddress.PostalCode,
			cancellationToken);
		var mailingAddressInput = GetAddressOrFallback(model.Contact.MailingAddress, model.Contact.PhysicalAddress);
		var resolvedMailingAddress = await ResolveOrCreatePostalAddressAsync(
			mailingAddressInput.CountryId,
			mailingAddressInput.StateProvinceId,
			mailingAddressInput.CountyId,
			mailingAddressInput.AddressLine1,
			mailingAddressInput.AddressLine2,
			mailingAddressInput.City,
			mailingAddressInput.PostalCode,
			cancellationToken);
		var normalizedPhoneLabel = ContactOptionCatalog.NormalizePhoneType(model.Contact.PhoneLabel);
		var normalizedBestTimeToContact = ContactOptionCatalog.NormalizeBestTime(model.Contact.BestTimeToContact);
		var normalizedPreferredContactMethod = ContactOptionCatalog.NormalizePreferredContactMethod(model.Contact.PreferredContactMethod);
		var normalizedEmailLabel = ContactOptionCatalog.NormalizeEmailType(model.Contact.EmailLabel);

		var effectiveSubmittedByUserId = model.IsStaffMode ? await RequireCurrentUserIdAsync(cancellationToken) : null;
		var response = new SurveyResponse(
			assignment.Id,
			effectiveSubmittedByUserId,
			model.IsStaffMode,
			model.Contact.FirstName,
			model.Contact.MiddleName,
			model.Contact.LastName,
			resolvedPhysicalAddress.PostalAddress.Id,
			model.Contact.PhysicalAddress.AddressLine1,
			model.Contact.PhysicalAddress.AddressLine2,
			model.Contact.PhysicalAddress.City,
			resolvedPhysicalAddress.StateProvince.Code,
			model.Contact.PhysicalAddress.PostalCode,
			resolvedMailingAddress.PostalAddress.Id,
			mailingAddressInput.AddressLine1,
			mailingAddressInput.AddressLine2,
			mailingAddressInput.City,
			resolvedMailingAddress.StateProvince.Code,
			mailingAddressInput.PostalCode,
			resolvedPhysicalAddress.County?.FipsCode,
			resolvedPhysicalAddress.County?.Name,
			resolvedPhysicalAddress.StateProvince.Code,
			model.Contact.PhoneNumber,
			normalizedPhoneLabel,
			normalizedBestTimeToContact,
			normalizedPreferredContactMethod,
			model.Contact.Email,
			normalizedEmailLabel,
			assignment.SurveyVersion.SurveyDefinition.Name,
			assignment.SurveyVersion.DisplayName,
			resolvedPhysicalAddress.Country.Name,
			resolvedMailingAddress.Country.Name);

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
		await AuditTenantEntityChangeAsync(
			"tenant.response.created",
			nameof(SurveyResponse),
			response.Id,
			$"Survey response was submitted for assignment '{assignment.PublicToken}'.",
			cancellationToken);

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
			.IgnoreQueryFilters()
			.Include(assignment => assignment.Location)
				.ThenInclude(location => location.Person)
					.ThenInclude(person => person.PostalAddress)
			.Include(assignment => assignment.Location)
				.ThenInclude(location => location.Person)
					.ThenInclude(person => person.MailingPostalAddress)
			.Include(assignment => assignment.Location)
				.ThenInclude(location => location.Person)
					.ThenInclude(person => person.Phones)
			.Include(assignment => assignment.Location)
				.ThenInclude(location => location.Person)
					.ThenInclude(person => person.Emails)
			.Include(assignment => assignment.Location)
				.ThenInclude(location => location.PostalAddress)
			.Include(assignment => assignment.Location)
				.ThenInclude(location => location.MailingPostalAddress)
			.Include(assignment => assignment.Location)
				.ThenInclude(location => location.Phones)
			.Include(assignment => assignment.Location)
				.ThenInclude(location => location.Emails)
			.Include(assignment => assignment.LocationPhone)
			.Include(assignment => assignment.LocationEmail)
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
			Contact = await BuildRespondentContactModelAsync(assignment.Location.Person, assignment.Location, assignment.LocationPhone, assignment.LocationEmail, cancellationToken),
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

	private async Task<IReadOnlyList<SelectOption>> GetPersonSelectOptionsAsync(CancellationToken cancellationToken, int? includePersonId = null)
	{
		return await _dbContext.People
			.AsNoTracking()
			.Where(person => !person.IsArchived || (includePersonId.HasValue && person.Id == includePersonId.Value))
			.OrderBy(person => person.LastName)
			.ThenBy(person => person.FirstName)
			.Select(person => new SelectOption
			{
				Value = person.Id.ToString(),
				Label = $"{person.LastName}, {person.FirstName} ({person.Email})"
			})
			.ToListAsync(cancellationToken);
	}

	private async Task<int?> GetAssignmentPersonIdAsync(int assignmentId, CancellationToken cancellationToken)
	{
		return await _dbContext.SurveyAssignments
			.AsNoTracking()
			.Where(assignment => assignment.Id == assignmentId)
			.Select(assignment => (int?)assignment.Location.PersonId)
			.FirstOrDefaultAsync(cancellationToken);
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

	private static int? GetFirstOptionId(IReadOnlyList<SelectOption> options)
	{
		var value = options.Select(option => TryParseInt(option.Value)).FirstOrDefault(item => item > 0);
		return value > 0 ? value : null;
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
