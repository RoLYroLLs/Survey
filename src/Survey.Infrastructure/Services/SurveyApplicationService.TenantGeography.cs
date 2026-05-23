using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Domain;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService
{
	public async Task<TenantGeographyVisibilityEditModel> GetTenantGeographyVisibilityAsync(CancellationToken cancellationToken = default)
	{
		await RequireTenantAdminOrOwnerAsync(cancellationToken);

		return new TenantGeographyVisibilityEditModel
		{
			VisibleCountryIds = await _dbContext.TenantVisibleCountries
				.AsNoTracking()
				.OrderBy(item => item.CountryId)
				.Select(item => item.CountryId)
				.ToListAsync(cancellationToken),
			VisibleStateProvinceIds = await _dbContext.TenantVisibleStateProvinces
				.AsNoTracking()
				.OrderBy(item => item.StateProvinceId)
				.Select(item => item.StateProvinceId)
				.ToListAsync(cancellationToken),
			VisibleCountyIds = await _dbContext.TenantVisibleCounties
				.AsNoTracking()
				.OrderBy(item => item.CountyId)
				.Select(item => item.CountyId)
				.ToListAsync(cancellationToken),
			CountryOptions = await GetCountrySelectOptionsAsync(cancellationToken),
			StateProvinceOptions = await GetStateProvinceSelectOptionsAsync(null, cancellationToken),
			CountyOptions = await GetCountySelectOptionsAsync(null, cancellationToken)
		};
	}

	public async Task SaveTenantGeographyVisibilityAsync(TenantGeographyVisibilityEditModel model, CancellationToken cancellationToken = default)
	{
		var tenantContext = await RequireTenantAdminOrOwnerAsync(cancellationToken);
		var tenantId = tenantContext.TenantId ?? throw new UnauthorizedAccessException("An active tenant is required.");

		var visibleCountryIds = model.VisibleCountryIds
			.Where(static id => id > 0)
			.Distinct()
			.OrderBy(static id => id)
			.ToList();
		var visibleStateProvinceIds = model.VisibleStateProvinceIds
			.Where(static id => id > 0)
			.Distinct()
			.OrderBy(static id => id)
			.ToList();
		var visibleCountyIds = model.VisibleCountyIds
			.Where(static id => id > 0)
			.Distinct()
			.OrderBy(static id => id)
			.ToList();

		await EnsureVisibleGeographySelectionExistsAsync(_dbContext.Countries, visibleCountryIds, "country", cancellationToken);
		await EnsureVisibleGeographySelectionExistsAsync(_dbContext.StateProvinces, visibleStateProvinceIds, "state or territory", cancellationToken);
		await EnsureVisibleGeographySelectionExistsAsync(_dbContext.Counties, visibleCountyIds, "county", cancellationToken);

		var existingCountries = await _dbContext.TenantVisibleCountries.ToListAsync(cancellationToken);
		var existingStates = await _dbContext.TenantVisibleStateProvinces.ToListAsync(cancellationToken);
		var existingCounties = await _dbContext.TenantVisibleCounties.ToListAsync(cancellationToken);

		_dbContext.TenantVisibleCountries.RemoveRange(existingCountries);
		_dbContext.TenantVisibleStateProvinces.RemoveRange(existingStates);
		_dbContext.TenantVisibleCounties.RemoveRange(existingCounties);

		foreach (var countryId in visibleCountryIds)
		{
			_dbContext.TenantVisibleCountries.Add(new TenantVisibleCountry(tenantId, countryId));
		}

		foreach (var stateProvinceId in visibleStateProvinceIds)
		{
			_dbContext.TenantVisibleStateProvinces.Add(new TenantVisibleStateProvince(tenantId, stateProvinceId));
		}

		foreach (var countyId in visibleCountyIds)
		{
			_dbContext.TenantVisibleCounties.Add(new TenantVisibleCounty(tenantId, countyId));
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		await AuditTenantEntityChangeAsync(
			"tenant.geography.visibility.updated",
			nameof(Tenant),
			tenantId,
			$"Tenant geography visibility was updated (countries: {visibleCountryIds.Count}, states: {visibleStateProvinceIds.Count}, counties: {visibleCountyIds.Count}).",
			cancellationToken);
	}

	public async Task<IReadOnlyList<SelectOption>> GetTenantStateProvinceSelectOptionsAsync(int? countryId, int? includeStateProvinceId = null, CancellationToken cancellationToken = default)
	{
		var scope = await GetTenantGeographyScopeAsync(cancellationToken);
		var query = _dbContext.StateProvinces
			.AsNoTracking()
			.Include(stateProvince => stateProvince.Country)
			.Where(stateProvince => stateProvince.Counties.Any())
			.AsQueryable();

		if (countryId.HasValue && countryId.Value > 0)
		{
			query = query.Where(stateProvince => stateProvince.CountryId == countryId.Value);
		}

		query = ApplyTenantStateProvinceVisibility(query, scope, includeStateProvinceId);

		return await query
			.OrderBy(stateProvince => stateProvince.Country.Name)
			.ThenBy(stateProvince => stateProvince.Name)
			.Select(stateProvince => new SelectOption
			{
				Value = stateProvince.Id.ToString(),
				Label = $"{stateProvince.Country.Iso2Code} - {stateProvince.Name} ({stateProvince.Code})"
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<SelectOption>> GetTenantCountySelectOptionsAsync(int? stateProvinceId, int? includeCountyId = null, CancellationToken cancellationToken = default)
	{
		var scope = await GetTenantGeographyScopeAsync(cancellationToken);
		var query = _dbContext.Counties
			.AsNoTracking()
			.Include(county => county.StateProvince)
				.ThenInclude(stateProvince => stateProvince.Country)
			.AsQueryable();

		if (stateProvinceId.HasValue && stateProvinceId.Value > 0)
		{
			query = query.Where(county => county.StateProvinceId == stateProvinceId.Value);
		}

		query = ApplyTenantCountyVisibility(query, scope, includeCountyId);

		return await query
			.OrderBy(county => county.StateProvince.Code)
			.ThenBy(county => county.Name)
			.Select(county => new SelectOption
			{
				Value = county.Id.ToString(),
				Label = $"{county.StateProvince.Code} - {county.Name}"
			})
			.ToListAsync(cancellationToken);
	}

	private async Task<IReadOnlyList<SelectOption>> GetTenantCountrySelectOptionsAsync(int? includeCountryId, CancellationToken cancellationToken)
	{
		var scope = await GetTenantGeographyScopeAsync(cancellationToken);
		var query = _dbContext.Countries
			.AsNoTracking()
			.Where(country => country.StateProvinces.Any(stateProvince => stateProvince.Counties.Any()))
			.AsQueryable();

		query = ApplyTenantCountryVisibility(query, scope, includeCountryId);

		return await query
			.OrderBy(country => country.Name)
			.Select(country => new SelectOption
			{
				Value = country.Id.ToString(),
				Label = $"{country.Name} ({country.Iso2Code})"
			})
			.ToListAsync(cancellationToken);
	}

	private IQueryable<Country> ApplyTenantCountryVisibility(IQueryable<Country> query, TenantGeographyScope scope, int? includeCountryId)
	{
		if (!scope.IsRestricted)
		{
			return query;
		}

		var hasVisibleStateProvinces = scope.StateProvinceIds.Length > 0;
		var hasVisibleCounties = scope.CountyIds.Length > 0;

		return query.Where(country =>
			scope.CountryIds.Contains(country.Id)
			|| hasVisibleStateProvinces && country.StateProvinces.Any(stateProvince => scope.StateProvinceIds.Contains(stateProvince.Id))
			|| hasVisibleCounties && country.StateProvinces.Any(stateProvince => stateProvince.Counties.Any(county => scope.CountyIds.Contains(county.Id)))
			|| includeCountryId.HasValue && country.Id == includeCountryId.Value);
	}

	private IQueryable<StateProvince> ApplyTenantStateProvinceVisibility(IQueryable<StateProvince> query, TenantGeographyScope scope, int? includeStateProvinceId)
	{
		if (!scope.IsRestricted)
		{
			return query;
		}

		var hasVisibleCounties = scope.CountyIds.Length > 0;

		return query.Where(stateProvince =>
			scope.CountryIds.Contains(stateProvince.CountryId)
			|| scope.StateProvinceIds.Contains(stateProvince.Id)
			|| hasVisibleCounties && stateProvince.Counties.Any(county => scope.CountyIds.Contains(county.Id))
			|| includeStateProvinceId.HasValue && stateProvince.Id == includeStateProvinceId.Value);
	}

	private IQueryable<County> ApplyTenantCountyVisibility(IQueryable<County> query, TenantGeographyScope scope, int? includeCountyId)
	{
		if (!scope.IsRestricted)
		{
			return query;
		}

		return query.Where(county =>
			scope.CountryIds.Contains(county.StateProvince.CountryId)
			|| scope.StateProvinceIds.Contains(county.StateProvinceId)
			|| scope.CountyIds.Contains(county.Id)
			|| includeCountyId.HasValue && county.Id == includeCountyId.Value);
	}

	private async Task<TenantGeographyScope> GetTenantGeographyScopeAsync(CancellationToken cancellationToken)
	{
		var tenantId = _tenantExecutionContext.TenantId;
		if (!tenantId.HasValue)
		{
			var context = await _tenantContextAccessor.GetCurrentAsync(cancellationToken);
			tenantId = context.TenantId;
		}

		if (!tenantId.HasValue || tenantId.Value <= 0)
		{
			return TenantGeographyScope.Unrestricted;
		}

		var countryIds = await _dbContext.TenantVisibleCountries
			.AsNoTracking()
			.IgnoreQueryFilters()
			.Where(item => item.TenantId == tenantId.Value)
			.Select(item => item.CountryId)
			.ToArrayAsync(cancellationToken);
		var stateProvinceIds = await _dbContext.TenantVisibleStateProvinces
			.AsNoTracking()
			.IgnoreQueryFilters()
			.Where(item => item.TenantId == tenantId.Value)
			.Select(item => item.StateProvinceId)
			.ToArrayAsync(cancellationToken);
		var countyIds = await _dbContext.TenantVisibleCounties
			.AsNoTracking()
			.IgnoreQueryFilters()
			.Where(item => item.TenantId == tenantId.Value)
			.Select(item => item.CountyId)
			.ToArrayAsync(cancellationToken);

		if (countryIds.Length == 0 && stateProvinceIds.Length == 0 && countyIds.Length == 0)
		{
			return TenantGeographyScope.Unrestricted;
		}

		return new TenantGeographyScope(countryIds, stateProvinceIds, countyIds);
	}

	private async Task EnsureTenantCountryVisibleAsync(int countryId, CancellationToken cancellationToken)
	{
		var scope = await GetTenantGeographyScopeAsync(cancellationToken);
		if (!scope.IsRestricted)
		{
			return;
		}

		var isVisible = await ApplyTenantCountryVisibility(
				_dbContext.Countries.AsNoTracking().Where(country => country.Id == countryId),
				scope,
				null)
			.AnyAsync(cancellationToken);
		if (!isVisible)
		{
			throw new InvalidOperationException("The selected country is not available for this tenant.");
		}
	}

	private async Task EnsureTenantStateProvinceVisibleAsync(int countryId, int stateProvinceId, CancellationToken cancellationToken)
	{
		var scope = await GetTenantGeographyScopeAsync(cancellationToken);
		if (!scope.IsRestricted)
		{
			return;
		}

		var isVisible = await ApplyTenantStateProvinceVisibility(
				_dbContext.StateProvinces
					.AsNoTracking()
					.Where(stateProvince => stateProvince.Id == stateProvinceId && stateProvince.CountryId == countryId),
				scope,
				null)
			.AnyAsync(cancellationToken);
		if (!isVisible)
		{
			throw new InvalidOperationException("The selected state or territory is not available for this tenant.");
		}
	}

	private async Task EnsureTenantCountyVisibleAsync(int stateProvinceId, int countyId, CancellationToken cancellationToken)
	{
		var scope = await GetTenantGeographyScopeAsync(cancellationToken);
		if (!scope.IsRestricted)
		{
			return;
		}

		var isVisible = await ApplyTenantCountyVisibility(
				_dbContext.Counties
					.AsNoTracking()
					.Include(county => county.StateProvince)
					.Where(county => county.Id == countyId && county.StateProvinceId == stateProvinceId),
				scope,
				null)
			.AnyAsync(cancellationToken);
		if (!isVisible)
		{
			throw new InvalidOperationException("The selected county is not available for this tenant.");
		}
	}

	private async Task EnsureVisibleGeographySelectionExistsAsync<TEntity>(
		DbSet<TEntity> dbSet,
		IReadOnlyCollection<int> ids,
		string label,
		CancellationToken cancellationToken)
		where TEntity : class
	{
		if (ids.Count == 0)
		{
			return;
		}

		var existingIds = await dbSet
			.AsNoTracking()
			.Where(entity => ids.Contains(EF.Property<int>(entity, "Id")))
			.Select(entity => EF.Property<int>(entity, "Id"))
			.ToListAsync(cancellationToken);
		if (existingIds.Count != ids.Count)
		{
			throw new InvalidOperationException($"One or more selected {label} values were not found.");
		}
	}

	private sealed class TenantGeographyScope
	{
		public static TenantGeographyScope Unrestricted { get; } = new([], [], []);

		public TenantGeographyScope(int[] countryIds, int[] stateProvinceIds, int[] countyIds)
		{
			CountryIds = countryIds;
			StateProvinceIds = stateProvinceIds;
			CountyIds = countyIds;
		}

		public int[] CountryIds { get; }
		public int[] StateProvinceIds { get; }
		public int[] CountyIds { get; }
		public bool IsRestricted => CountryIds.Length > 0 || StateProvinceIds.Length > 0 || CountyIds.Length > 0;
	}
}
