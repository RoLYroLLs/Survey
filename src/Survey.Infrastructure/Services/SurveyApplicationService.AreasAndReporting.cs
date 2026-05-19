using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Survey.Application.Models;
using Survey.Domain;
using Survey.Infrastructure.Identity;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService
{
	public async Task<PagedResult<AreaListItem>> GetAreasAsync(PagedQuery request, int? countyId = null, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.AreasView, cancellationToken);

		var query = _dbContext.Areas
			.AsNoTracking()
			.Include(area => area.Counties)
			.Include(area => area.Goals)
			.AsQueryable();

		string? countyNameFilter = null;
		if (countyId.HasValue)
		{
			var scope = await GetTenantGeographyScopeAsync(cancellationToken);
			var county = await ApplyTenantCountyVisibility(
					_dbContext.Counties
						.AsNoTracking()
						.Include(entity => entity.StateProvince)
						.Where(entity => entity.Id == countyId.Value),
					scope,
					null)
				.FirstOrDefaultAsync(cancellationToken)
				?? throw new InvalidOperationException("The selected county was not found.");
			countyNameFilter = county.Name;
			query = query.Where(area => area.Counties.Any(areaCounty => areaCounty.CountyFips == county.FipsCode));
		}

		var itemsQuery = query
			.OrderBy(area => area.Name)
			.ThenBy(area => area.Id)
			.Select(area => new AreaListItem
			{
				Id = area.Id,
				Name = area.Name,
				Description = area.Description,
				CountyCount = area.Counties.Count,
				GoalCount = area.Goals.Count,
				UpdatedUtc = area.UpdatedUtc,
				CountyNameFilter = countyNameFilter
			});

		var sortMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["name"] = [nameof(AreaListItem.Name)],
			["description"] = [nameof(AreaListItem.Description)],
			["counties"] = [nameof(AreaListItem.CountyCount)],
			["goals"] = [nameof(AreaListItem.GoalCount)],
			["updated"] = [nameof(AreaListItem.UpdatedUtc)]
		};

		return await BuildPagedResultAsync(itemsQuery, request, sortMap, nameof(AreaListItem.Id), cancellationToken);
	}

	public async Task<AreaEditModel> GetAreaAsync(int? id, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.AreasView, cancellationToken);

		if (!id.HasValue)
		{
			return new AreaEditModel
			{
				CountyOptions = await GetCountyOptionsAsync(null, cancellationToken)
			};
		}

		var area = await _dbContext.Areas
			.AsNoTracking()
			.Include(entity => entity.Counties)
			.FirstOrDefaultAsync(entity => entity.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested area was not found.");

		return new AreaEditModel
		{
			Id = area.Id,
			Name = area.Name,
			Description = area.Description,
			SelectedCountyFips = area.Counties
				.OrderBy(county => county.StateCode)
				.ThenBy(county => county.CountyName)
				.Select(county => county.CountyFips)
				.ToList(),
			CountyOptions = await GetCountyOptionsAsync(area.Id, cancellationToken)
		};
	}

	public async Task<int> SaveAreaAsync(AreaEditModel model, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(model.Id.HasValue ? TenantPermissionKeys.AreasEdit : TenantPermissionKeys.AreasCreate, cancellationToken);
		var isNew = !model.Id.HasValue;

		var selectedCountyFips = model.SelectedCountyFips
			.Where(static countyFips => !string.IsNullOrWhiteSpace(countyFips))
			.Select(static countyFips => countyFips.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		if (selectedCountyFips.Count > 0)
		{
			var conflicts = await _dbContext.AreaCounties
				.AsNoTracking()
				.Include(county => county.Area)
				.Where(county => selectedCountyFips.Contains(county.CountyFips) && (!model.Id.HasValue || county.AreaId != model.Id.Value))
				.Select(county => new { county.CountyFips, county.Area.Name })
				.ToListAsync(cancellationToken);

			if (conflicts.Count > 0)
			{
				var details = string.Join(", ", conflicts.Select(conflict => $"{conflict.CountyFips} ({conflict.Name})"));
				throw new InvalidOperationException($"Counties can only belong to one area. Remove these assignments first: {details}.");
			}
		}

		Area area;
		Dictionary<string, (string CountyName, string StateCode)> existingCountyMetadata;
		if (model.Id.HasValue)
		{
			area = await _dbContext.Areas
				.Include(entity => entity.Counties)
				.FirstOrDefaultAsync(entity => entity.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested area was not found.");
			existingCountyMetadata = area.Counties.ToDictionary(
				county => county.CountyFips,
				county => (county.CountyName, county.StateCode),
				StringComparer.OrdinalIgnoreCase);
			area.Update(model.Name, model.Description);
			_dbContext.AreaCounties.RemoveRange(area.Counties);
		}
		else
		{
			area = new Area(model.Name, model.Description);
			existingCountyMetadata = new Dictionary<string, (string CountyName, string StateCode)>(StringComparer.OrdinalIgnoreCase);
			_dbContext.Areas.Add(area);
			await _dbContext.SaveChangesAsync(cancellationToken);
		}

		var countyMetadata = await GetCountyMetadataLookupAsync(model.Id, cancellationToken);
		foreach (var countyFips in selectedCountyFips)
		{
			if (!countyMetadata.TryGetValue(countyFips, out var metadata) && !existingCountyMetadata.TryGetValue(countyFips, out metadata))
			{
				throw new InvalidOperationException($"The selected county '{countyFips}' was not found in the counties list.");
			}

			_dbContext.AreaCounties.Add(new AreaCounty(area.Id, countyFips, metadata.CountyName, metadata.StateCode));
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		await AuditTenantEntityChangeAsync(
			isNew ? "tenant.area.created" : "tenant.area.updated",
			nameof(Area),
			area.Id,
			$"Area '{area.Name}' was {(isNew ? "created" : "saved")} with {selectedCountyFips.Count} county assignments.",
			cancellationToken);
		return area.Id;
	}

	public async Task<PagedResult<ZipCountyMappingListItem>> GetZipCountyMappingsAsync(PagedQuery request, string? search = null, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.GeographyView, cancellationToken);

		var query = _dbContext.ZipCountyLookups
			.AsNoTracking()
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim().ToUpperInvariant();
			query = query.Where(mapping =>
				mapping.ZipCode.ToUpper().Contains(term) ||
				mapping.CountyFips.ToUpper().Contains(term) ||
				mapping.CountyName.ToUpper().Contains(term) ||
				mapping.StateCode.ToUpper().Contains(term));
		}

		var items = await query
			.Select(mapping => new ZipCountyMappingListItem
			{
				Id = mapping.Id,
				ZipCode = mapping.ZipCode,
				CountyFips = mapping.CountyFips,
				CountyName = mapping.CountyName,
				StateCode = mapping.StateCode,
				ResidentialRatio = mapping.ResidentialRatio
			})
			.ToListAsync(cancellationToken);

		var sortMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["zip"] = [nameof(ZipCountyMappingListItem.ZipCode)],
			["county"] = [nameof(ZipCountyMappingListItem.CountyName)],
			["countyfips"] = [nameof(ZipCountyMappingListItem.CountyFips)],
			["state"] = [nameof(ZipCountyMappingListItem.StateCode)],
			["ratio"] = [nameof(ZipCountyMappingListItem.ResidentialRatio)]
		};
		var orderedItems = items
			.OrderBy(item => item.ZipCode)
			.ThenByDescending(item => item.ResidentialRatio)
			.ThenBy(item => item.CountyName)
			.ThenBy(item => item.Id)
			.AsQueryable();
		var normalizedRequest = NormalizePagedQuery(request);
		var sortedItems = ApplyRequestedSorts(orderedItems, request, sortMap, nameof(ZipCountyMappingListItem.Id)).ToList();
		var pagedItems = sortedItems
			.Skip(normalizedRequest.Offset)
			.Take(normalizedRequest.Limit)
			.ToList();

		return CreatePagedResult(pagedItems, sortedItems.Count, normalizedRequest.Offset);
	}

	public async Task<ZipCountyMappingEditModel> GetZipCountyMappingAsync(int? id, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.GeographyView, cancellationToken);

		if (!id.HasValue)
		{
			return new ZipCountyMappingEditModel
			{
				StateCode = "FL",
				ResidentialRatio = 1m
			};
		}

		var mapping = await _dbContext.ZipCountyLookups
			.AsNoTracking()
			.FirstOrDefaultAsync(entity => entity.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested ZIP mapping was not found.");

		return new ZipCountyMappingEditModel
		{
			Id = mapping.Id,
			ZipCode = mapping.ZipCode,
			CountyFips = mapping.CountyFips,
			CountyName = mapping.CountyName,
			StateCode = mapping.StateCode,
			ResidentialRatio = mapping.ResidentialRatio
		};
	}

	public async Task<int> SaveZipCountyMappingAsync(ZipCountyMappingEditModel model, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.GeographyManage, cancellationToken);

		ZipCountyLookup entity;
		if (model.Id.HasValue)
		{
			entity = await _dbContext.ZipCountyLookups.FirstOrDefaultAsync(mapping => mapping.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested ZIP mapping was not found.");
			entity.Update(model.ZipCode, model.CountyFips, model.CountyName, model.StateCode, model.ResidentialRatio);
		}
		else
		{
			entity = new ZipCountyLookup(model.ZipCode, model.CountyFips, model.CountyName, model.StateCode, model.ResidentialRatio);
			_dbContext.ZipCountyLookups.Add(entity);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	public async Task<ZipCountyImportResultModel> ImportZipCountyMappingsAsync(ZipCountyImportModel model, CancellationToken cancellationToken = default)
	{
		await RequirePlatformPermissionAsync(PlatformPermissionKeys.GeographyManage, cancellationToken);

		if (string.IsNullOrWhiteSpace(model.CsvContent))
		{
			throw new InvalidOperationException("Upload or paste a CSV file before importing.");
		}

		var importedRows = ParseZipCountyCsv(model.CsvContent)
			.GroupBy(row => BuildZipCountyKey(row.ZipCode, row.CountyFips), StringComparer.OrdinalIgnoreCase)
			.Select(group => group
				.OrderByDescending(row => row.ResidentialRatio)
				.ThenBy(row => row.CountyName, StringComparer.OrdinalIgnoreCase)
				.First())
			.ToList();

		if (importedRows.Count == 0)
		{
			throw new InvalidOperationException("No ZIP-to-county rows were found in the uploaded CSV.");
		}

		if (model.ReplaceExisting)
		{
			var existingRows = await _dbContext.ZipCountyLookups.ToListAsync(cancellationToken);
			_dbContext.ZipCountyLookups.RemoveRange(existingRows);
		}

		var existingLookup = model.ReplaceExisting
			? new Dictionary<string, ZipCountyLookup>(StringComparer.OrdinalIgnoreCase)
			: await _dbContext.ZipCountyLookups
				.ToDictionaryAsync(
					mapping => BuildZipCountyKey(mapping.ZipCode, mapping.CountyFips),
					mapping => mapping,
					StringComparer.OrdinalIgnoreCase,
					cancellationToken);

		foreach (var row in importedRows)
		{
			var key = BuildZipCountyKey(row.ZipCode, row.CountyFips);
			if (existingLookup.TryGetValue(key, out var existing))
			{
				existing.Update(row.ZipCode, row.CountyFips, row.CountyName, row.StateCode, row.ResidentialRatio);
				continue;
			}

			var entity = new ZipCountyLookup(row.ZipCode, row.CountyFips, row.CountyName, row.StateCode, row.ResidentialRatio);
			_dbContext.ZipCountyLookups.Add(entity);
			existingLookup[key] = entity;
		}

		await _dbContext.SaveChangesAsync(cancellationToken);

		return new ZipCountyImportResultModel
		{
			ImportedRowCount = importedRows.Count,
			DistinctZipCount = importedRows.Select(row => row.ZipCode).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
			DistinctCountyCount = importedRows.Select(row => row.CountyFips).Distinct(StringComparer.OrdinalIgnoreCase).Count()
		};
	}

	public async Task<PagedResult<GoalListItem>> GetGoalsAsync(PagedQuery request, string? userId = null, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.GoalsView, cancellationToken);

		var query = _dbContext.Goals
			.AsNoTracking()
			.Include(goal => goal.Area)
			.Include(goal => goal.SurveyDefinition)
			.OrderBy(goal => goal.EndDate)
			.ThenBy(goal => goal.Name)
			.ThenBy(goal => goal.Id);
		var normalizedRequest = NormalizePagedQuery(request);
		var totalCount = await query.CountAsync(cancellationToken);
		var goals = totalCount == 0
			? []
			: await query.ToListAsync(cancellationToken);

		var progressLookup = await BuildGoalProgressLookupAsync(goals, cancellationToken);
		var favoriteGoalIds = await GetFavoriteGoalIdSetAsync(await RequireCurrentUserIdAsync(cancellationToken), cancellationToken);

		var items = goals
			.Select(goal =>
			{
				var progress = progressLookup[goal.Id];
				return new GoalListItem
				{
					Id = goal.Id,
					Name = goal.Name,
					AreaName = goal.Area?.Name,
					SurveyName = goal.SurveyDefinition?.Name,
					TargetResponseCount = goal.TargetResponseCount,
					CompletedResponses = progress.CompletedResponses,
					RemainingResponses = progress.RemainingResponses,
					ProgressPercent = progress.ProgressPercent,
					StartDate = goal.StartDate,
					EndDate = goal.EndDate,
					IsFavorite = favoriteGoalIds.Contains(goal.Id)
				};
			})
			.ToList();
		var sortMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["name"] = [nameof(GoalListItem.Name)],
			["area"] = [nameof(GoalListItem.AreaName)],
			["survey"] = [nameof(GoalListItem.SurveyName)],
			["window"] = [nameof(GoalListItem.StartDate), nameof(GoalListItem.EndDate)],
			["progress"] = [nameof(GoalListItem.ProgressPercent), nameof(GoalListItem.CompletedResponses), nameof(GoalListItem.TargetResponseCount)],
			["dashboard"] = [nameof(GoalListItem.IsFavorite)]
		};
		var orderedItems = ApplyRequestedSorts(items.AsQueryable(), request, sortMap, nameof(GoalListItem.Id)).ToList();
		var pagedItems = orderedItems
			.Skip(normalizedRequest.Offset)
			.Take(normalizedRequest.Limit)
			.ToList();

		return CreatePagedResult(pagedItems, totalCount, normalizedRequest.Offset);
	}

	public async Task<IReadOnlyList<DashboardFavoriteGoalItem>> GetDashboardFavoriteGoalsAsync(string userId, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.DashboardView, cancellationToken);
		var favoriteGoalIds = await GetFavoriteGoalIdSetAsync(await RequireCurrentUserIdAsync(cancellationToken), cancellationToken);
		if (favoriteGoalIds.Count == 0)
		{
			return [];
		}

		var goals = await _dbContext.Goals
			.AsNoTracking()
			.Where(goal => favoriteGoalIds.Contains(goal.Id))
			.OrderBy(goal => goal.EndDate)
			.ThenBy(goal => goal.Name)
			.ToListAsync(cancellationToken);

		if (goals.Count == 0)
		{
			return [];
		}

		var progressLookup = await BuildGoalProgressLookupAsync(goals, cancellationToken);

		return goals
			.Select(goal =>
			{
				var progress = progressLookup[goal.Id];
				return new DashboardFavoriteGoalItem
				{
					GoalId = goal.Id,
					GoalName = goal.Name,
					ProgressPercent = progress.ProgressPercent,
					CompletedResponses = progress.CompletedResponses,
					TargetResponseCount = goal.TargetResponseCount
				};
			})
			.ToList();
	}

	public async Task ToggleFavoriteGoalAsync(int goalId, string userId, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.DashboardView, cancellationToken);
		var currentUserId = await RequireCurrentUserIdAsync(cancellationToken);

		var goalExists = await _dbContext.Goals.AnyAsync(goal => goal.Id == goalId, cancellationToken);
		if (!goalExists)
		{
			throw new InvalidOperationException("The requested goal was not found.");
		}

		var user = await _userManager.FindByIdAsync(currentUserId)
			?? throw new InvalidOperationException("The signed-in user could not be found.");

		var favoriteGoalIds = user.GetFavoriteGoalIds();
		if (!favoriteGoalIds.Add(goalId))
		{
			favoriteGoalIds.Remove(goalId);
		}

		user.SetFavoriteGoalIds(favoriteGoalIds);

		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded)
		{
			var message = result.Errors.FirstOrDefault()?.Description ?? "The favorite goal setting could not be saved.";
			throw new InvalidOperationException(message);
		}
	}

	public async Task<GoalEditModel> GetGoalAsync(int? id, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.GoalsView, cancellationToken);

		var areaOptions = await GetAreaSelectOptionsAsync(cancellationToken);
		var surveyOptions = await GetSurveyDefinitionOptionsAsync(null, cancellationToken);
		if (!id.HasValue)
		{
			return new GoalEditModel
			{
				AreaId = null,
				AreaOptions = areaOptions,
				SurveyOptions = surveyOptions
			};
		}

		var goal = await _dbContext.Goals
			.AsNoTracking()
			.FirstOrDefaultAsync(entity => entity.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested goal was not found.");

		return new GoalEditModel
		{
			Id = goal.Id,
			Name = goal.Name,
			Description = goal.Description,
			AreaId = goal.AreaId,
			SurveyDefinitionId = goal.SurveyDefinitionId,
			TargetResponseCount = goal.TargetResponseCount,
			StartDate = goal.StartDate,
			EndDate = goal.EndDate,
			AreaOptions = areaOptions,
			SurveyOptions = surveyOptions
		};
	}

	public async Task<int> SaveGoalAsync(GoalEditModel model, CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(model.Id.HasValue ? TenantPermissionKeys.GoalsEdit : TenantPermissionKeys.GoalsCreate, cancellationToken);
		var isNew = !model.Id.HasValue;

		if (model.AreaId.HasValue)
		{
			await EnsureAreaExistsAsync(model.AreaId.Value, cancellationToken);
		}
		if (model.SurveyDefinitionId.HasValue)
		{
			await EnsureSurveyDefinitionExistsAsync(model.SurveyDefinitionId.Value, cancellationToken);
		}

		Goal goal;
		if (model.Id.HasValue)
		{
			goal = await _dbContext.Goals.FirstOrDefaultAsync(entity => entity.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested goal was not found.");
			goal.Update(model.Name, model.Description, model.AreaId, model.SurveyDefinitionId, model.TargetResponseCount, model.StartDate, model.EndDate);
		}
		else
		{
			goal = new Goal(model.Name, model.Description, model.AreaId, model.SurveyDefinitionId, model.TargetResponseCount, model.StartDate, model.EndDate);
			_dbContext.Goals.Add(goal);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		await AuditTenantEntityChangeAsync(
			isNew ? "tenant.goal.created" : "tenant.goal.updated",
			nameof(Goal),
			goal.Id,
			$"Goal '{goal.Name}' was {(isNew ? "created" : "saved")}.",
			cancellationToken);
		return goal.Id;
	}

	public async Task<ReportingOverviewModel> GetReportingOverviewAsync(CancellationToken cancellationToken = default)
	{
		await RequireTenantPermissionAsync(TenantPermissionKeys.ReportsView, cancellationToken);

		var areas = await _dbContext.Areas
			.AsNoTracking()
			.Include(area => area.Counties)
			.Include(area => area.Goals)
			.OrderBy(area => area.Name)
			.ToListAsync(cancellationToken);
		var goals = await _dbContext.Goals
			.AsNoTracking()
			.Include(goal => goal.Area)
			.Include(goal => goal.SurveyDefinition)
			.OrderBy(goal => goal.EndDate)
			.ThenBy(goal => goal.Name)
			.ToListAsync(cancellationToken);
		var responseFacts = await GetResponseFactsAsync(cancellationToken);
		var zipMappingCount = await _dbContext.ZipCountyLookups.CountAsync(cancellationToken);

		var areaResponseCounts = areas.ToDictionary(
			area => area.Id,
			area => responseFacts.Count(response => !string.IsNullOrWhiteSpace(response.CountyFips) && area.Counties.Any(county => county.CountyFips == response.CountyFips)));
		var goalProgress = await BuildGoalProgressLookupAsync(goals, cancellationToken, responseFacts);

		return new ReportingOverviewModel
		{
			TotalResponses = responseFacts.Count,
			MappedResponses = responseFacts.Count(response => !string.IsNullOrWhiteSpace(response.CountyFips)),
			UnmappedResponses = responseFacts.Count(response => string.IsNullOrWhiteSpace(response.CountyFips)),
			GoalCount = goals.Count,
			ZipMappingCount = zipMappingCount,
			AreaResponses = areas
				.Select(area => new AreaResponseReportItem
				{
					AreaId = area.Id,
					AreaName = area.Name,
					ResponseCount = areaResponseCounts.GetValueOrDefault(area.Id),
					GoalCount = area.Goals.Count
				})
				.ToList(),
			GoalProgress = goals
				.Select(goal =>
				{
					var progress = goalProgress[goal.Id];
					return new GoalProgressReportItem
					{
						GoalId = goal.Id,
						GoalName = goal.Name,
						AreaName = goal.Area?.Name,
						SurveyName = goal.SurveyDefinition?.Name,
						TargetResponseCount = goal.TargetResponseCount,
						CompletedResponses = progress.CompletedResponses,
						RemainingResponses = progress.RemainingResponses,
						ProgressPercent = progress.ProgressPercent,
						StartDate = goal.StartDate,
						EndDate = goal.EndDate
					};
				})
				.ToList(),
			UnmappedPostalCodes = responseFacts
				.Where(response => string.IsNullOrWhiteSpace(response.CountyFips))
				.GroupBy(response => string.IsNullOrWhiteSpace(response.PostalCode) ? "Missing" : response.PostalCode!, StringComparer.OrdinalIgnoreCase)
				.OrderByDescending(group => group.Count())
				.ThenBy(group => group.Key)
				.Take(25)
				.Select(group => new UnmappedPostalCodeReportItem
				{
					PostalCode = group.Key,
					ResponseCount = group.Count()
				})
				.ToList()
		};
	}

	private async Task<IReadOnlyList<CountyOptionItem>> GetCountyOptionsAsync(int? includeAreaId, CancellationToken cancellationToken)
	{
		var scope = await GetTenantGeographyScopeAsync(cancellationToken);
		var zipCounts = await _dbContext.ZipCountyLookups
			.AsNoTracking()
			.GroupBy(mapping => mapping.CountyFips)
			.ToDictionaryAsync(
				group => group.Key,
				group => group.Select(mapping => mapping.ZipCode).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
				StringComparer.OrdinalIgnoreCase,
				cancellationToken);
		var importedCounties = await ApplyTenantCountyVisibility(
				_dbContext.Counties
					.AsNoTracking()
					.Include(county => county.StateProvince)
					.AsQueryable(),
				scope,
				null)
			.Select(county => new CountyOptionItem
			{
				CountyFips = county.FipsCode,
				CountyName = county.Name,
				StateCode = county.StateProvince.Code,
				ZipCount = 0
			})
			.ToListAsync(cancellationToken);

		foreach (var county in importedCounties)
		{
			county.ZipCount = zipCounts.GetValueOrDefault(county.CountyFips);
		}

		if (!includeAreaId.HasValue)
		{
			return importedCounties
				.OrderBy(option => option.StateCode)
				.ThenBy(option => option.CountyName)
				.ToList();
		}

		var existingCounties = await _dbContext.AreaCounties
			.AsNoTracking()
			.Where(county => county.AreaId == includeAreaId.Value)
			.ToListAsync(cancellationToken);

		foreach (var existingCounty in existingCounties)
		{
			if (importedCounties.Any(option => option.CountyFips == existingCounty.CountyFips))
			{
				continue;
			}

			importedCounties.Add(new CountyOptionItem
			{
				CountyFips = existingCounty.CountyFips,
				CountyName = existingCounty.CountyName,
				StateCode = existingCounty.StateCode,
				ZipCount = 0
			});
		}

		return importedCounties
			.OrderBy(option => option.StateCode)
			.ThenBy(option => option.CountyName)
			.ToList();
	}

	private async Task<Dictionary<string, (string CountyName, string StateCode)>> GetCountyMetadataLookupAsync(int? includeAreaId, CancellationToken cancellationToken)
	{
		var scope = await GetTenantGeographyScopeAsync(cancellationToken);
		var counties = await ApplyTenantCountyVisibility(
				_dbContext.Counties
					.AsNoTracking()
					.Include(county => county.StateProvince)
					.AsQueryable(),
				scope,
				null)
			.ToListAsync(cancellationToken);
		var lookup = counties
			.ToDictionary(
				county => county.FipsCode,
				county => (county.Name, county.StateProvince.Code),
				StringComparer.OrdinalIgnoreCase);
		if (!includeAreaId.HasValue)
		{
			return lookup;
		}

		var existingCounties = await _dbContext.AreaCounties
			.AsNoTracking()
			.Where(county => county.AreaId == includeAreaId.Value)
			.ToListAsync(cancellationToken);
		foreach (var county in existingCounties)
		{
			if (!lookup.ContainsKey(county.CountyFips))
			{
				lookup[county.CountyFips] = (county.CountyName, county.StateCode);
			}
		}

		return lookup;
	}

	private async Task<IReadOnlyList<SelectOption>> GetAreaSelectOptionsAsync(CancellationToken cancellationToken)
	{
		var options = await _dbContext.Areas
			.AsNoTracking()
			.OrderBy(area => area.Name)
			.Select(area => new SelectOption
			{
				Value = area.Id.ToString(),
				Label = area.Name
			})
			.ToListAsync(cancellationToken)
			;

		options.Insert(0, new SelectOption
		{
			Value = string.Empty,
			Label = "All Areas"
		});

		return options;
	}

	private async Task EnsureAreaExistsAsync(int areaId, CancellationToken cancellationToken)
	{
		var exists = await _dbContext.Areas.AnyAsync(area => area.Id == areaId, cancellationToken);
		if (!exists)
		{
			throw new InvalidOperationException("The selected area was not found.");
		}
	}

	private async Task<Dictionary<int, GoalProgressSnapshot>> BuildGoalProgressLookupAsync(
		IReadOnlyList<Goal> goals,
		CancellationToken cancellationToken,
		List<ResponseFact>? responseFacts = null)
	{
		responseFacts ??= await GetResponseFactsAsync(cancellationToken);
		var areaCountyLookup = await _dbContext.AreaCounties
			.AsNoTracking()
			.GroupBy(county => county.AreaId)
			.ToDictionaryAsync(
				group => group.Key,
				group => group.Select(county => county.CountyFips).ToHashSet(StringComparer.OrdinalIgnoreCase),
				cancellationToken);

		return goals.ToDictionary(
			goal => goal.Id,
			goal =>
			{
				var goalCountyFips = goal.AreaId.HasValue
					? areaCountyLookup.GetValueOrDefault(goal.AreaId.Value)
						?? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
					: null;
				var completed = responseFacts.Count(response =>
					(goalCountyFips is null || goalCountyFips.Contains(response.CountyFips ?? string.Empty)) &&
					response.SubmittedDate >= goal.StartDate &&
					response.SubmittedDate <= goal.EndDate &&
					(!goal.SurveyDefinitionId.HasValue || response.SurveyDefinitionId == goal.SurveyDefinitionId.Value));
				var remaining = Math.Max(0, goal.TargetResponseCount - completed);
				var progressPercent = Math.Round((decimal)completed / goal.TargetResponseCount * 100m, 1, MidpointRounding.AwayFromZero);

				return new GoalProgressSnapshot(completed, remaining, progressPercent);
			});
	}

	private async Task<List<ResponseFact>> GetResponseFactsAsync(CancellationToken cancellationToken)
	{
		var responses = await _dbContext.SurveyResponses
			.AsNoTracking()
			.Include(response => response.SurveyAssignment)
				.ThenInclude(assignment => assignment.SurveyVersion)
			.ToListAsync(cancellationToken);
		var primaryCountyLookup = await GetPrimaryCountyLookupByZipAsync(cancellationToken);

		return responses
			.Select(response =>
			{
				var postalCode = response.RespondentPostalCode ?? PostalCodeNormalizer.Extract(response.RespondentHomeAddress);
				var resolvedCounty = !string.IsNullOrWhiteSpace(response.RespondentCountyFipsSnapshot)
					? new ResolvedCounty(
						response.RespondentCountyFipsSnapshot,
						response.RespondentCountyNameSnapshot,
						response.RespondentStateCodeSnapshot)
					: ResolveCounty(postalCode, primaryCountyLookup);

				return new ResponseFact(
					response.Id,
					response.SurveyAssignment.SurveyVersion.SurveyDefinitionId,
					DateOnly.FromDateTime(response.SubmittedUtc.UtcDateTime),
					postalCode,
					resolvedCounty.CountyFips,
					resolvedCounty.CountyName,
					resolvedCounty.StateCode);
			})
			.ToList();
	}

	private async Task<Dictionary<string, ResolvedCounty>> GetPrimaryCountyLookupByZipAsync(CancellationToken cancellationToken)
	{
		var mappings = await _dbContext.ZipCountyLookups
			.AsNoTracking()
			.ToListAsync(cancellationToken);

		return mappings
			.GroupBy(mapping => mapping.ZipCode, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(
				group => group.Key,
				group =>
				{
					var primary = group
						.OrderByDescending(mapping => mapping.ResidentialRatio)
						.ThenBy(mapping => mapping.CountyName, StringComparer.OrdinalIgnoreCase)
						.First();
					return new ResolvedCounty(primary.CountyFips, primary.CountyName, primary.StateCode);
				},
				StringComparer.OrdinalIgnoreCase);
	}

	private static ResolvedCounty ResolveCounty(string? postalCode, IReadOnlyDictionary<string, ResolvedCounty> primaryCountyLookup)
	{
		if (string.IsNullOrWhiteSpace(postalCode))
		{
			return new ResolvedCounty(null, null, null);
		}

		return primaryCountyLookup.TryGetValue(postalCode, out var resolvedCounty)
			? resolvedCounty
			: new ResolvedCounty(null, null, null);
	}

	private static List<ImportedZipCountyRow> ParseZipCountyCsv(string csvContent)
	{
		using var reader = new StringReader(csvContent);
		using var parser = new TextFieldParser(reader)
		{
			TextFieldType = FieldType.Delimited,
			HasFieldsEnclosedInQuotes = true,
			TrimWhiteSpace = true
		};
		parser.SetDelimiters(",");

		if (parser.EndOfData)
		{
			return [];
		}

		var headers = parser.ReadFields() ?? [];
		var headerLookup = headers
			.Select((header, index) => new { Header = header?.Trim() ?? string.Empty, Index = index })
			.Where(item => !string.IsNullOrWhiteSpace(item.Header))
			.ToDictionary(item => item.Header, item => item.Index, StringComparer.OrdinalIgnoreCase);

		var zipIndex = FindRequiredHeader(headerLookup, "ZIP", "ZIPCODE", "ZIP_CODE");
		var countyFipsIndex = FindRequiredHeader(headerLookup, "COUNTY", "COUNTYFIPS", "COUNTY_FIPS");
		var countyNameIndex = FindOptionalHeader(headerLookup, "COUNTYNAME", "COUNTY_NAME");
		var stateCodeIndex = FindOptionalHeader(headerLookup, "STATE", "STATECODE", "STATE_CODE", "USPS_ZIP_PREF_STATE");
		var residentialRatioIndex = FindOptionalHeader(headerLookup, "RES_RATIO", "RESIDENTIALRATIO", "RESIDENTIAL_RATIO");

		var rows = new List<ImportedZipCountyRow>();
		while (!parser.EndOfData)
		{
			var fields = parser.ReadFields();
			if (fields is null || fields.All(string.IsNullOrWhiteSpace))
			{
				continue;
			}

			var zipCode = GetField(fields, zipIndex);
			var countyFips = GetField(fields, countyFipsIndex);
			if (string.IsNullOrWhiteSpace(zipCode) || string.IsNullOrWhiteSpace(countyFips))
			{
				continue;
			}

			var countyName = GetField(fields, countyNameIndex) ?? countyFips.Trim();
			var stateCode = ResolveStateCode(GetField(fields, stateCodeIndex), countyFips);
			var residentialRatio = ParseDecimal(GetField(fields, residentialRatioIndex));

			rows.Add(new ImportedZipCountyRow(
				PostalCodeNormalizer.Normalize(zipCode, nameof(zipCode)) ?? string.Empty,
				countyFips.Trim(),
				string.IsNullOrWhiteSpace(countyName) ? countyFips.Trim() : countyName.Trim(),
				stateCode,
				residentialRatio));
		}

		return rows;
	}

	private static int FindRequiredHeader(IReadOnlyDictionary<string, int> headerLookup, params string[] names)
	{
		var index = FindOptionalHeader(headerLookup, names);
		return index ?? throw new InvalidOperationException($"The imported CSV must include one of these columns: {string.Join(", ", names)}.");
	}

	private static int? FindOptionalHeader(IReadOnlyDictionary<string, int> headerLookup, params string[] names)
	{
		foreach (var name in names)
		{
			if (headerLookup.TryGetValue(name, out var index))
			{
				return index;
			}
		}

		return null;
	}

	private static string? GetField(string[] fields, int? index)
	{
		if (!index.HasValue || index.Value < 0 || index.Value >= fields.Length)
		{
			return null;
		}

		return fields[index.Value];
	}

	private static decimal ParseDecimal(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return 1m;
		}

		return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
			? parsed
			: 1m;
	}

	private static string ResolveStateCode(string? stateCode, string countyFips)
	{
		if (!string.IsNullOrWhiteSpace(stateCode))
		{
			return stateCode.Trim().ToUpperInvariant();
		}

		var stateFips = countyFips?.Trim();
		if (!string.IsNullOrWhiteSpace(stateFips) && stateFips.Length >= 2)
		{
			var key = stateFips[..2];
			if (StateCodeLookup.TryGetValue(key, out var resolved))
			{
				return resolved;
			}
		}

		return "NA";
	}

	private static string BuildZipCountyKey(string zipCode, string countyFips)
	{
		return $"{zipCode.Trim().ToUpperInvariant()}|{countyFips.Trim().ToUpperInvariant()}";
	}

	private async Task<HashSet<int>> GetFavoriteGoalIdSetAsync(string? userId, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(userId))
		{
			return [];
		}

		var serializedFavoriteGoals = await _dbContext.Users
			.AsNoTracking()
			.Where(user => user.Id == userId)
			.Select(user => user.FavoriteGoalIds)
			.FirstOrDefaultAsync(cancellationToken);

		return ApplicationUser.ParseFavoriteGoalIds(serializedFavoriteGoals);
	}

	private sealed record ImportedZipCountyRow(
		string ZipCode,
		string CountyFips,
		string CountyName,
		string StateCode,
		decimal ResidentialRatio);

	private sealed record GoalProgressSnapshot(
		int CompletedResponses,
		int RemainingResponses,
		decimal ProgressPercent);

	private sealed record ResponseFact(
		int ResponseId,
		int SurveyDefinitionId,
		DateOnly SubmittedDate,
		string? PostalCode,
		string? CountyFips,
		string? CountyName,
		string? StateCode);

	private sealed record ResolvedCounty(
		string? CountyFips,
		string? CountyName,
		string? StateCode);

	private static readonly IReadOnlyDictionary<string, string> StateCodeLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["01"] = "AL",
		["02"] = "AK",
		["04"] = "AZ",
		["05"] = "AR",
		["06"] = "CA",
		["08"] = "CO",
		["09"] = "CT",
		["10"] = "DE",
		["11"] = "DC",
		["12"] = "FL",
		["13"] = "GA",
		["15"] = "HI",
		["16"] = "ID",
		["17"] = "IL",
		["18"] = "IN",
		["19"] = "IA",
		["20"] = "KS",
		["21"] = "KY",
		["22"] = "LA",
		["23"] = "ME",
		["24"] = "MD",
		["25"] = "MA",
		["26"] = "MI",
		["27"] = "MN",
		["28"] = "MS",
		["29"] = "MO",
		["30"] = "MT",
		["31"] = "NE",
		["32"] = "NV",
		["33"] = "NH",
		["34"] = "NJ",
		["35"] = "NM",
		["36"] = "NY",
		["37"] = "NC",
		["38"] = "ND",
		["39"] = "OH",
		["40"] = "OK",
		["41"] = "OR",
		["42"] = "PA",
		["44"] = "RI",
		["45"] = "SC",
		["46"] = "SD",
		["47"] = "TN",
		["48"] = "TX",
		["49"] = "UT",
		["50"] = "VT",
		["51"] = "VA",
		["53"] = "WA",
		["54"] = "WV",
		["55"] = "WI",
		["56"] = "WY",
		["72"] = "PR"
	};
}
