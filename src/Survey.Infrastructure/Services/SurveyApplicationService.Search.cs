using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Domain;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService
{
	public async Task<TenantSearchResultModel> SearchTenantAsync(string query, CancellationToken cancellationToken = default)
	{
		var context = await RequireTenantAccessAsync(cancellationToken);
		var normalizedQuery = query.Trim();
		if (string.IsNullOrWhiteSpace(normalizedQuery))
		{
			return new TenantSearchResultModel
			{
				Query = string.Empty
			};
		}

		var sections = new List<TenantSearchSectionModel>();

		if (context.TenantPermissions.Contains(TenantPermissionKeys.PeopleView, StringComparer.Ordinal))
		{
			var peopleQuery = _dbContext.People
				.AsNoTracking()
				.Where(person =>
					!person.IsArchived &&
					((person.FirstName ?? string.Empty).Contains(normalizedQuery)
					|| (person.LastName ?? string.Empty).Contains(normalizedQuery)
					|| person.Email.Contains(normalizedQuery)
					|| person.PhoneNumber.Contains(normalizedQuery)));
			var totalCount = await peopleQuery.CountAsync(cancellationToken);
			if (totalCount > 0)
			{
				var rawItems = await peopleQuery
					.OrderBy(person => person.LastName)
					.ThenBy(person => person.FirstName)
					.ThenBy(person => person.Id)
					.Take(5)
					.Select(person => new
					{
						person.Id,
						person.FirstName,
						person.LastName,
						person.Email
					})
					.ToListAsync(cancellationToken);
				var items = rawItems
					.Select(person => new TenantSearchItemModel
					{
						Title = BuildDisplayName(person.FirstName, person.LastName, person.Email),
						Subtitle = person.Email,
						Url = $"/app/people/{person.Id}",
						IconName = "people"
					})
					.ToList();

				sections.Add(new TenantSearchSectionModel
				{
					Key = "people",
					Title = "People",
					ViewAllUrl = "/app/people",
					TotalCount = totalCount,
					Items = items
				});
			}
		}

		if (context.TenantPermissions.Contains(TenantPermissionKeys.LocationsView, StringComparer.Ordinal))
		{
			var locationsQuery = _dbContext.Locations
				.AsNoTracking()
				.Include(location => location.Person)
				.Where(location =>
					location.Nickname.Contains(normalizedQuery)
					|| (location.City ?? string.Empty).Contains(normalizedQuery)
					|| location.Email.Contains(normalizedQuery)
					|| location.PhoneNumber.Contains(normalizedQuery)
					|| (location.Person.FirstName ?? string.Empty).Contains(normalizedQuery)
					|| (location.Person.LastName ?? string.Empty).Contains(normalizedQuery));
			var totalCount = await locationsQuery.CountAsync(cancellationToken);
			if (totalCount > 0)
			{
				var rawItems = await locationsQuery
					.OrderBy(location => location.Nickname)
					.ThenBy(location => location.Id)
					.Take(5)
					.Select(location => new
					{
						location.Id,
						location.PersonId,
						location.Nickname,
						location.City,
						location.Person.FirstName,
						location.Person.LastName,
						location.Person.Email
					})
					.ToListAsync(cancellationToken);
				var items = rawItems
					.Select(location => new TenantSearchItemModel
					{
						Title = location.Nickname,
						Subtitle = $"{BuildDisplayName(location.FirstName, location.LastName, location.Email)} - {location.City}",
						Url = $"/app/people/{location.PersonId}/locations/{location.Id}",
						IconName = "location"
					})
					.ToList();

				sections.Add(new TenantSearchSectionModel
				{
					Key = "locations",
					Title = "Locations",
					ViewAllUrl = "/app/people",
					TotalCount = totalCount,
					Items = items
				});
			}
		}

		if (context.TenantPermissions.Contains(TenantPermissionKeys.AssignmentsView, StringComparer.Ordinal))
		{
			var assignmentsQuery = _dbContext.SurveyAssignments
				.AsNoTracking()
				.Include(assignment => assignment.Location)
					.ThenInclude(location => location.Person)
				.Include(assignment => assignment.SurveyVersion)
					.ThenInclude(version => version.SurveyDefinition)
				.Where(assignment =>
					assignment.PublicToken.Contains(normalizedQuery)
					|| assignment.Location.Nickname.Contains(normalizedQuery)
					|| (assignment.Location.Person.FirstName ?? string.Empty).Contains(normalizedQuery)
					|| (assignment.Location.Person.LastName ?? string.Empty).Contains(normalizedQuery)
					|| assignment.SurveyVersion.DisplayName.Contains(normalizedQuery)
					|| assignment.SurveyVersion.SurveyDefinition.Name.Contains(normalizedQuery));
			var totalCount = await assignmentsQuery.CountAsync(cancellationToken);
			if (totalCount > 0)
			{
				var assignmentItemsQuery = assignmentsQuery
					.Select(assignment => new
					{
						assignment.Id,
						assignment.IsArchived,
						assignment.ExpiresAtUtc,
						assignment.CreatedUtc,
						HasResponse = assignment.Response != null,
						SurveyName = assignment.SurveyVersion.SurveyDefinition.Name,
						LocationName = assignment.Location.Nickname,
						assignment.Location.Person.FirstName,
						assignment.Location.Person.LastName,
						assignment.Location.Person.Email
					});
				var rawItems = _dbContext.Database.IsSqlite()
					? (await assignmentItemsQuery.ToListAsync(cancellationToken))
						.OrderByDescending(assignment => assignment.CreatedUtc)
						.ThenByDescending(assignment => assignment.Id)
						.Take(5)
						.ToList()
					: await assignmentItemsQuery
						.OrderByDescending(assignment => assignment.CreatedUtc)
						.ThenByDescending(assignment => assignment.Id)
						.Take(5)
						.ToListAsync(cancellationToken);
				var items = rawItems
					.Select(assignment => new TenantSearchItemModel
					{
						Title = $"{assignment.SurveyName} - {assignment.LocationName}",
						Subtitle = $"{BuildDisplayName(assignment.FirstName, assignment.LastName, assignment.Email)} - {FormatAssignmentState(assignment.IsArchived, assignment.HasResponse, assignment.ExpiresAtUtc)}",
						Url = $"/app/assignments/{assignment.Id}",
						IconName = "assignments"
					})
					.ToList();

				sections.Add(new TenantSearchSectionModel
				{
					Key = "assignments",
					Title = "Assignments",
					ViewAllUrl = "/app/assignments",
					TotalCount = totalCount,
					Items = items
				});
			}
		}

		if (context.TenantPermissions.Contains(TenantPermissionKeys.SurveysView, StringComparer.Ordinal))
		{
			var surveysQuery = _dbContext.SurveyDefinitions
				.AsNoTracking()
				.Where(definition =>
					!definition.IsArchived &&
					(definition.Name.Contains(normalizedQuery)
					|| (definition.Description != null && definition.Description.Contains(normalizedQuery))));
			var totalCount = await surveysQuery.CountAsync(cancellationToken);
			if (totalCount > 0)
			{
				var items = await surveysQuery
					.OrderBy(definition => definition.Name)
					.ThenBy(definition => definition.Id)
					.Take(5)
					.Select(definition => new TenantSearchItemModel
					{
						Title = definition.Name,
						Subtitle = definition.Description,
						Url = $"/app/surveys/{definition.Id}",
						IconName = "surveys"
					})
					.ToListAsync(cancellationToken);

				sections.Add(new TenantSearchSectionModel
				{
					Key = "surveys",
					Title = "Surveys",
					ViewAllUrl = "/app/surveys",
					TotalCount = totalCount,
					Items = items
				});
			}

			var versionsQuery = _dbContext.SurveyVersions
				.AsNoTracking()
				.Include(version => version.SurveyDefinition)
				.Where(version =>
					!version.IsArchived &&
					(version.DisplayName.Contains(normalizedQuery)
					|| version.VersionNumber.ToString().Contains(normalizedQuery)
					|| version.SurveyDefinition.Name.Contains(normalizedQuery)
					|| (version.SurveyDefinition.Description != null && version.SurveyDefinition.Description.Contains(normalizedQuery))));
			var versionTotalCount = await versionsQuery.CountAsync(cancellationToken);
			if (versionTotalCount > 0)
			{
				var versionItems = await versionsQuery
					.OrderBy(version => version.SurveyDefinition.Name)
					.ThenBy(version => version.VersionNumber)
					.ThenBy(version => version.Id)
					.Take(5)
					.Select(version => new TenantSearchItemModel
					{
						Title = version.DisplayName,
						Subtitle = $"{version.SurveyDefinition.Name} - Version {version.VersionNumber}" + (version.IsPublished ? " - Published" : string.Empty),
						Url = $"/app/versions/{version.Id}",
						IconName = "versions"
					})
					.ToListAsync(cancellationToken);

				sections.Add(new TenantSearchSectionModel
				{
					Key = "versions",
					Title = "Versions",
					ViewAllUrl = "/app/versions",
					TotalCount = versionTotalCount,
					Items = versionItems
				});
			}
		}

		if (context.TenantPermissions.Contains(TenantPermissionKeys.GoalsView, StringComparer.Ordinal))
		{
			var goalsQuery = _dbContext.Goals
				.AsNoTracking()
				.Where(goal =>
					goal.Name.Contains(normalizedQuery)
					|| (goal.Description != null && goal.Description.Contains(normalizedQuery)));
			var totalCount = await goalsQuery.CountAsync(cancellationToken);
			if (totalCount > 0)
			{
				var items = await goalsQuery
					.OrderBy(goal => goal.Name)
					.ThenBy(goal => goal.Id)
					.Take(5)
					.Select(goal => new TenantSearchItemModel
					{
						Title = goal.Name,
						Subtitle = goal.Description,
						Url = $"/app/goals/{goal.Id}",
						IconName = "goals"
					})
					.ToListAsync(cancellationToken);

				sections.Add(new TenantSearchSectionModel
				{
					Key = "goals",
					Title = "Goals",
					ViewAllUrl = "/app/goals",
					TotalCount = totalCount,
					Items = items
				});
			}
		}

		if (context.TenantPermissions.Contains(TenantPermissionKeys.AreasView, StringComparer.Ordinal))
		{
			var areasQuery = _dbContext.Areas
				.AsNoTracking()
				.Where(area =>
					area.Name.Contains(normalizedQuery)
					|| (area.Description != null && area.Description.Contains(normalizedQuery)));
			var totalCount = await areasQuery.CountAsync(cancellationToken);
			if (totalCount > 0)
			{
				var items = await areasQuery
					.OrderBy(area => area.Name)
					.ThenBy(area => area.Id)
					.Take(5)
					.Select(area => new TenantSearchItemModel
					{
						Title = area.Name,
						Subtitle = area.Description,
						Url = $"/app/areas/{area.Id}",
						IconName = "areas"
					})
					.ToListAsync(cancellationToken);

				sections.Add(new TenantSearchSectionModel
				{
					Key = "areas",
					Title = "Areas",
					ViewAllUrl = "/app/areas",
					TotalCount = totalCount,
					Items = items
				});
			}
		}

		if (context.TenantPermissions.Contains(TenantPermissionKeys.UsersView, StringComparer.Ordinal))
		{
			var usersResult = await GetTenantUsersAsync(new PagedQuery { Offset = 0, Limit = 5 }, normalizedQuery, cancellationToken);
			if (usersResult.TotalCount > 0)
			{
				sections.Add(new TenantSearchSectionModel
				{
					Key = "users",
					Title = "Users",
					ViewAllUrl = "/app/users",
					TotalCount = usersResult.TotalCount,
					Items = usersResult.Items
						.Select(item => new TenantSearchItemModel
						{
							Title = item.FullName,
							Subtitle = $"{item.Email} - {item.Role}",
							Url = $"/app/users/{item.MembershipId}",
							IconName = "users"
						})
						.ToList()
				});
			}
		}

		if (context.TenantPermissions.Contains(TenantPermissionKeys.ReportsView, StringComparer.Ordinal)
			&& ReportSearchTerms.Any(term => term.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
				|| normalizedQuery.Contains(term, StringComparison.OrdinalIgnoreCase)))
		{
			sections.Add(new TenantSearchSectionModel
			{
				Key = "reports",
				Title = "Reports",
				ViewAllUrl = "/app/reports",
				TotalCount = 1,
				Items =
				[
					new TenantSearchItemModel
					{
						Title = "Reports",
						Subtitle = "Response totals, goal progress, area counts, and unmapped ZIP activity.",
						Url = "/app/reports",
						IconName = "reports"
					}
				]
			});
		}

		if (context.TenantPermissions.Contains(TenantPermissionKeys.ResponsesView, StringComparer.Ordinal))
		{
			var responsesQuery = _dbContext.SurveyResponses
				.AsNoTracking()
				.Include(response => response.SurveyAssignment)
				.Where(response =>
					response.RespondentFirstName.Contains(normalizedQuery)
					|| response.RespondentLastName.Contains(normalizedQuery)
					|| response.SurveyNameSnapshot.Contains(normalizedQuery)
					|| response.SurveyVersionNameSnapshot.Contains(normalizedQuery)
					|| (response.RespondentEmail != null && response.RespondentEmail.Contains(normalizedQuery)));
			var totalCount = await responsesQuery.CountAsync(cancellationToken);
			if (totalCount > 0)
			{
				var responseItemsQuery = responsesQuery
					.Select(response => new
					{
						response.Id,
						response.RespondentFirstName,
						response.RespondentMiddleName,
						response.RespondentLastName,
						response.SurveyNameSnapshot,
						response.SubmittedUtc
					});
				var rawItems = _dbContext.Database.IsSqlite()
					? (await responseItemsQuery.ToListAsync(cancellationToken))
						.OrderByDescending(response => response.SubmittedUtc)
						.ThenByDescending(response => response.Id)
						.Take(5)
						.ToList()
					: await responseItemsQuery
						.OrderByDescending(response => response.SubmittedUtc)
						.ThenByDescending(response => response.Id)
						.Take(5)
						.ToListAsync(cancellationToken);
				var items = rawItems
					.Select(response => new TenantSearchItemModel
					{
						Title = BuildFullName(response.RespondentFirstName, response.RespondentMiddleName, response.RespondentLastName),
						Subtitle = $"{response.SurveyNameSnapshot} - {response.SubmittedUtc:MMM d, yyyy}",
						Url = $"/app/responses/{response.Id}",
						IconName = "responses"
					})
					.ToList();

				sections.Add(new TenantSearchSectionModel
				{
					Key = "responses",
					Title = "Responses",
					ViewAllUrl = "/app/responses",
					TotalCount = totalCount,
					Items = items
				});
			}
		}

		return new TenantSearchResultModel
		{
			Query = normalizedQuery,
			Sections = sections
		};
	}

	private static string FormatAssignmentState(bool isArchived, bool hasResponse, DateTimeOffset? expiresAtUtc)
	{
		if (isArchived)
		{
			return "Archived";
		}

		if (hasResponse)
		{
			return "Completed";
		}

		return expiresAtUtc.HasValue && expiresAtUtc.Value <= DateTimeOffset.UtcNow ? "Expired" : "Active";
	}

	private static readonly string[] ReportSearchTerms =
	[
		"report",
		"reports",
		"reporting",
		"response totals",
		"goal progress",
		"area counts",
		"unmapped zip",
		"zip activity"
	];
}
