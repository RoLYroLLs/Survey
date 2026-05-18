using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Survey.Domain;
using System.Reflection;

namespace Survey.Infrastructure.Persistence;

public sealed class GeographyDataSeeder(SurveyDbContext dbContext)
{
	private readonly SurveyDbContext _dbContext = dbContext;
	private const string GeographySeedKey = "geography.reference-data";
	private const int GeographySeedVersion = 2;

	public async Task SeedAsync(bool forceRun = false, CancellationToken cancellationToken = default)
	{
		var seedState = await _dbContext.SeedStates
			.FirstOrDefaultAsync(state => state.Key == GeographySeedKey, cancellationToken);

		if (!forceRun && seedState?.Version >= GeographySeedVersion)
		{
			return;
		}

		var unitedStates = await UpsertCountryAsync("United States of America", "US", "USA", cancellationToken);
		var stateLookup = await UpsertStateProvincesAsync(unitedStates.Id, cancellationToken);
		await UpsertUnitedStatesCountiesAsync(stateLookup, cancellationToken);

		if (stateLookup.TryGetValue("FL", out var floridaId))
		{
			await UpsertFloridaZipCountyMappingsAsync(floridaId, cancellationToken);
		}

		if (seedState is null)
		{
			_dbContext.SeedStates.Add(new SeedState(GeographySeedKey, GeographySeedVersion));
		}
		else
		{
			seedState.MarkApplied(GeographySeedVersion);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private async Task<Country> UpsertCountryAsync(string name, string iso2Code, string iso3Code, CancellationToken cancellationToken)
	{
		var entity = await _dbContext.Countries
			.FirstOrDefaultAsync(country => country.Iso2Code == iso2Code || country.Name == name, cancellationToken);

		if (entity is null)
		{
			entity = new Country(name, iso2Code, iso3Code);
			_dbContext.Countries.Add(entity);
		}
		else
		{
			entity.Update(name, iso2Code, iso3Code);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		return entity;
	}

	private async Task<Dictionary<string, int>> UpsertStateProvincesAsync(int countryId, CancellationToken cancellationToken)
	{
		var existingLookup = await _dbContext.StateProvinces
			.Where(stateProvince => stateProvince.CountryId == countryId)
			.ToDictionaryAsync(stateProvince => stateProvince.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

		foreach (var definition in UnitedStatesStates)
		{
			if (existingLookup.TryGetValue(definition.Code, out var existing))
			{
				existing.Update(countryId, definition.Name, definition.Code, definition.SubdivisionType);
				continue;
			}

			var entity = new StateProvince(countryId, definition.Name, definition.Code, definition.SubdivisionType);
			_dbContext.StateProvinces.Add(entity);
			existingLookup[definition.Code] = entity;
		}

		await _dbContext.SaveChangesAsync(cancellationToken);

		return await _dbContext.StateProvinces
			.AsNoTracking()
			.Where(stateProvince => stateProvince.CountryId == countryId)
			.ToDictionaryAsync(stateProvince => stateProvince.Code, stateProvince => stateProvince.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
	}

	private async Task UpsertUnitedStatesCountiesAsync(IReadOnlyDictionary<string, int> stateLookup, CancellationToken cancellationToken)
	{
		var existingLookup = await _dbContext.Counties
			.ToDictionaryAsync(
				county => BuildCountyKey(county.StateProvinceId, county.FipsCode),
				StringComparer.OrdinalIgnoreCase,
				cancellationToken);
		var stateNameLookup = UnitedStatesStates
			.ToDictionary(definition => definition.Name, definition => definition.Code, StringComparer.OrdinalIgnoreCase);

		foreach (var definition in LoadUnitedStatesCountySeed())
		{
			if (!stateNameLookup.TryGetValue(definition.StateName, out var stateCode)
				|| !stateLookup.TryGetValue(stateCode, out var stateProvinceId))
			{
				throw new InvalidOperationException($"The state '{definition.StateName}' was not found while seeding United States counties.");
			}

			var key = BuildCountyKey(stateProvinceId, definition.FipsCode);
			if (existingLookup.TryGetValue(key, out var existing))
			{
				existing.Update(stateProvinceId, definition.CountyName, definition.FipsCode);
				continue;
			}

			var entity = new County(stateProvinceId, definition.CountyName, definition.FipsCode);
			_dbContext.Counties.Add(entity);
			existingLookup[key] = entity;
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private async Task UpsertFloridaZipCountyMappingsAsync(int floridaStateProvinceId, CancellationToken cancellationToken)
	{
		var countyLookup = await _dbContext.Counties
			.AsNoTracking()
			.Where(county => county.StateProvinceId == floridaStateProvinceId)
			.ToDictionaryAsync(
				county => county.FipsCode,
				county => county,
				StringComparer.OrdinalIgnoreCase,
				cancellationToken);

		var existingLookup = await _dbContext.ZipCountyLookups
			.Where(mapping => mapping.StateCode == "FL")
			.ToDictionaryAsync(
				mapping => BuildZipCountyKey(mapping.ZipCode, mapping.CountyFips),
				StringComparer.OrdinalIgnoreCase,
				cancellationToken);

		foreach (var row in LoadFloridaZipCountySeed())
		{
			if (!countyLookup.TryGetValue(row.CountyFips, out var county))
			{
				throw new InvalidOperationException($"The Florida county FIPS '{row.CountyFips}' was not found while seeding ZIP mappings.");
			}

			var key = BuildZipCountyKey(row.ZipCode, county.FipsCode);
			if (existingLookup.TryGetValue(key, out var existing))
			{
				existing.Update(row.ZipCode, county.FipsCode, county.Name, "FL", row.ResidentialRatio);
				continue;
			}

			var entity = new ZipCountyLookup(row.ZipCode, county.FipsCode, county.Name, "FL", row.ResidentialRatio);
			_dbContext.ZipCountyLookups.Add(entity);
			existingLookup[key] = entity;
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private static IReadOnlyList<FloridaZipCountySeedRow> LoadFloridaZipCountySeed()
	{
		const string resourceName = "Survey.Infrastructure.Persistence.SeedData.FloridaZipCountySeed.csv";
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
			?? throw new InvalidOperationException($"The embedded seed resource '{resourceName}' was not found.");
		using var reader = new StreamReader(stream);

		var rows = new List<FloridaZipCountySeedRow>();
		var isFirstLine = true;
		while (!reader.EndOfStream)
		{
			var line = reader.ReadLine();
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			if (isFirstLine)
			{
				isFirstLine = false;
				continue;
			}

			var fields = line.Split(',');
			if (fields.Length != 5)
			{
				throw new InvalidOperationException($"The Florida ZIP seed row '{line}' is invalid.");
			}

			rows.Add(new FloridaZipCountySeedRow(
				fields[0].Trim(),
				fields[1].Trim(),
				fields[2].Trim().Trim('"'),
				decimal.Parse(fields[4].Trim(), CultureInfo.InvariantCulture)));
		}

		return rows;
	}

	private static IReadOnlyList<UsCountySeedRow> LoadUnitedStatesCountySeed()
	{
		const string resourceName = "Survey.Infrastructure.Persistence.SeedData.UsStateCountySeed.txt";
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
			?? throw new InvalidOperationException($"The embedded seed resource '{resourceName}' was not found.");
		using var reader = new StreamReader(stream);

		var rows = new List<UsCountySeedRow>();
		var lineIndex = 0;
		while (!reader.EndOfStream)
		{
			var line = reader.ReadLine();
			lineIndex++;
			if (lineIndex <= 4 || string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			var fields = line.Split('\t', StringSplitOptions.None);
			if (fields.Length < 4)
			{
				throw new InvalidOperationException($"The United States county seed row '{line}' is invalid.");
			}

			var stateFips = fields[0].Trim();
			var countyFips = fields[1].Trim();
			var stateName = fields[2].Trim();
			var countyName = fields[3].Trim();
			if (string.IsNullOrWhiteSpace(stateFips)
				|| string.IsNullOrWhiteSpace(countyFips)
				|| string.IsNullOrWhiteSpace(stateName)
				|| string.IsNullOrWhiteSpace(countyName)
				|| string.Equals(countyFips, "999", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			rows.Add(new UsCountySeedRow(
				stateName,
				$"{stateFips}{countyFips}",
				countyName));
		}

		return rows;
	}

	private static string BuildZipCountyKey(string zipCode, string countyFips)
	{
		return $"{zipCode.Trim().ToUpperInvariant()}|{countyFips.Trim().ToUpperInvariant()}";
	}

	private static string BuildCountyKey(int stateProvinceId, string countyFips)
	{
		return $"{stateProvinceId}|{countyFips.Trim().ToUpperInvariant()}";
	}

	private static readonly IReadOnlyList<(string Code, string Name, string SubdivisionType)> UnitedStatesStates =
	[
		("AL", "Alabama", "State"),
		("AK", "Alaska", "State"),
		("AZ", "Arizona", "State"),
		("AR", "Arkansas", "State"),
		("CA", "California", "State"),
		("CO", "Colorado", "State"),
		("CT", "Connecticut", "State"),
		("DE", "Delaware", "State"),
		("DC", "District of Columbia", "District"),
		("FL", "Florida", "State"),
		("GA", "Georgia", "State"),
		("HI", "Hawaii", "State"),
		("ID", "Idaho", "State"),
		("IL", "Illinois", "State"),
		("IN", "Indiana", "State"),
		("IA", "Iowa", "State"),
		("KS", "Kansas", "State"),
		("KY", "Kentucky", "State"),
		("LA", "Louisiana", "State"),
		("ME", "Maine", "State"),
		("MD", "Maryland", "State"),
		("MA", "Massachusetts", "State"),
		("MI", "Michigan", "State"),
		("MN", "Minnesota", "State"),
		("MS", "Mississippi", "State"),
		("MO", "Missouri", "State"),
		("MT", "Montana", "State"),
		("NE", "Nebraska", "State"),
		("NV", "Nevada", "State"),
		("NH", "New Hampshire", "State"),
		("NJ", "New Jersey", "State"),
		("NM", "New Mexico", "State"),
		("NY", "New York", "State"),
		("NC", "North Carolina", "State"),
		("ND", "North Dakota", "State"),
		("OH", "Ohio", "State"),
		("OK", "Oklahoma", "State"),
		("OR", "Oregon", "State"),
		("PA", "Pennsylvania", "State"),
		("RI", "Rhode Island", "State"),
		("SC", "South Carolina", "State"),
		("SD", "South Dakota", "State"),
		("TN", "Tennessee", "State"),
		("TX", "Texas", "State"),
		("UT", "Utah", "State"),
		("VT", "Vermont", "State"),
		("VA", "Virginia", "State"),
		("WA", "Washington", "State"),
		("WV", "West Virginia", "State"),
		("WI", "Wisconsin", "State"),
		("WY", "Wyoming", "State")
	];

	private sealed record FloridaZipCountySeedRow(
		string ZipCode,
		string CountyFips,
		string CountyName,
		decimal ResidentialRatio);

	private sealed record UsCountySeedRow(
		string StateName,
		string FipsCode,
		string CountyName);
}
