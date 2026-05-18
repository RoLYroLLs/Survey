using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Survey.Domain;
using System.Reflection;

namespace Survey.Infrastructure.Persistence;

internal sealed class GeographyDataSeeder(SurveyDbContext dbContext)
{
	private readonly SurveyDbContext _dbContext = dbContext;
	private const string GeographySeedKey = "geography.reference-data";
	private const int GeographySeedVersion = 1;

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

		if (stateLookup.TryGetValue("FL", out var floridaId))
		{
			await UpsertFloridaCountiesAsync(floridaId, cancellationToken);
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
				existing.Update(countryId, definition.Name, definition.Code, "State");
				continue;
			}

			var entity = new StateProvince(countryId, definition.Name, definition.Code, "State");
			_dbContext.StateProvinces.Add(entity);
			existingLookup[definition.Code] = entity;
		}

		await _dbContext.SaveChangesAsync(cancellationToken);

		return await _dbContext.StateProvinces
			.AsNoTracking()
			.Where(stateProvince => stateProvince.CountryId == countryId)
			.ToDictionaryAsync(stateProvince => stateProvince.Code, stateProvince => stateProvince.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
	}

	private async Task UpsertFloridaCountiesAsync(int floridaStateProvinceId, CancellationToken cancellationToken)
	{
		var existingLookup = await _dbContext.Counties
			.Where(county => county.StateProvinceId == floridaStateProvinceId)
			.ToDictionaryAsync(county => county.FipsCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

		foreach (var definition in FloridaCounties)
		{
			if (existingLookup.TryGetValue(definition.FipsCode, out var existing))
			{
				existing.Update(floridaStateProvinceId, definition.Name, definition.FipsCode);
				continue;
			}

			var entity = new County(floridaStateProvinceId, definition.Name, definition.FipsCode);
			_dbContext.Counties.Add(entity);
			existingLookup[definition.FipsCode] = entity;
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

	private static string BuildZipCountyKey(string zipCode, string countyFips)
	{
		return $"{zipCode.Trim().ToUpperInvariant()}|{countyFips.Trim().ToUpperInvariant()}";
	}

	private static readonly IReadOnlyList<(string Code, string Name)> UnitedStatesStates =
	[
		("AL", "Alabama"),
		("AK", "Alaska"),
		("AZ", "Arizona"),
		("AR", "Arkansas"),
		("CA", "California"),
		("CO", "Colorado"),
		("CT", "Connecticut"),
		("DE", "Delaware"),
		("FL", "Florida"),
		("GA", "Georgia"),
		("HI", "Hawaii"),
		("ID", "Idaho"),
		("IL", "Illinois"),
		("IN", "Indiana"),
		("IA", "Iowa"),
		("KS", "Kansas"),
		("KY", "Kentucky"),
		("LA", "Louisiana"),
		("ME", "Maine"),
		("MD", "Maryland"),
		("MA", "Massachusetts"),
		("MI", "Michigan"),
		("MN", "Minnesota"),
		("MS", "Mississippi"),
		("MO", "Missouri"),
		("MT", "Montana"),
		("NE", "Nebraska"),
		("NV", "Nevada"),
		("NH", "New Hampshire"),
		("NJ", "New Jersey"),
		("NM", "New Mexico"),
		("NY", "New York"),
		("NC", "North Carolina"),
		("ND", "North Dakota"),
		("OH", "Ohio"),
		("OK", "Oklahoma"),
		("OR", "Oregon"),
		("PA", "Pennsylvania"),
		("RI", "Rhode Island"),
		("SC", "South Carolina"),
		("SD", "South Dakota"),
		("TN", "Tennessee"),
		("TX", "Texas"),
		("UT", "Utah"),
		("VT", "Vermont"),
		("VA", "Virginia"),
		("WA", "Washington"),
		("WV", "West Virginia"),
		("WI", "Wisconsin"),
		("WY", "Wyoming")
	];

	private static readonly IReadOnlyList<(string FipsCode, string Name)> FloridaCounties =
	[
		("12001", "Alachua County"),
		("12003", "Baker County"),
		("12005", "Bay County"),
		("12007", "Bradford County"),
		("12009", "Brevard County"),
		("12011", "Broward County"),
		("12013", "Calhoun County"),
		("12015", "Charlotte County"),
		("12017", "Citrus County"),
		("12019", "Clay County"),
		("12021", "Collier County"),
		("12023", "Columbia County"),
		("12027", "DeSoto County"),
		("12029", "Dixie County"),
		("12031", "Duval County"),
		("12033", "Escambia County"),
		("12035", "Flagler County"),
		("12037", "Franklin County"),
		("12039", "Gadsden County"),
		("12041", "Gilchrist County"),
		("12043", "Glades County"),
		("12045", "Gulf County"),
		("12047", "Hamilton County"),
		("12049", "Hardee County"),
		("12051", "Hendry County"),
		("12053", "Hernando County"),
		("12055", "Highlands County"),
		("12057", "Hillsborough County"),
		("12059", "Holmes County"),
		("12061", "Indian River County"),
		("12063", "Jackson County"),
		("12065", "Jefferson County"),
		("12067", "Lafayette County"),
		("12069", "Lake County"),
		("12071", "Lee County"),
		("12073", "Leon County"),
		("12075", "Levy County"),
		("12077", "Liberty County"),
		("12079", "Madison County"),
		("12081", "Manatee County"),
		("12083", "Marion County"),
		("12085", "Martin County"),
		("12086", "Miami-Dade County"),
		("12087", "Monroe County"),
		("12089", "Nassau County"),
		("12091", "Okaloosa County"),
		("12093", "Okeechobee County"),
		("12095", "Orange County"),
		("12097", "Osceola County"),
		("12099", "Palm Beach County"),
		("12101", "Pasco County"),
		("12103", "Pinellas County"),
		("12105", "Polk County"),
		("12107", "Putnam County"),
		("12109", "St. Johns County"),
		("12111", "St. Lucie County"),
		("12113", "Santa Rosa County"),
		("12115", "Sarasota County"),
		("12117", "Seminole County"),
		("12119", "Sumter County"),
		("12121", "Suwannee County"),
		("12123", "Taylor County"),
		("12125", "Union County"),
		("12127", "Volusia County"),
		("12129", "Wakulla County"),
		("12131", "Walton County"),
		("12133", "Washington County")
	];

	private sealed record FloridaZipCountySeedRow(
		string ZipCode,
		string CountyFips,
		string CountyName,
		decimal ResidentialRatio);
}
