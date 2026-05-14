using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Survey.Application.Models;
using Survey.Domain;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService
{
	public async Task<IReadOnlyList<CountryListItem>> GetCountriesAsync(string? search = null, CancellationToken cancellationToken = default)
	{
		var query = _dbContext.Countries
			.AsNoTracking()
			.Include(country => country.StateProvinces)
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim().ToUpperInvariant();
			query = query.Where(country =>
				country.Name.ToUpper().Contains(term) ||
				country.Iso2Code.ToUpper().Contains(term) ||
				(country.Iso3Code != null && country.Iso3Code.ToUpper().Contains(term)));
		}

		return await query
			.OrderBy(country => country.Name)
			.Select(country => new CountryListItem
			{
				Id = country.Id,
				Name = country.Name,
				Iso2Code = country.Iso2Code,
				Iso3Code = country.Iso3Code,
				StateProvinceCount = country.StateProvinces.Count
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<CountryEditModel> GetCountryAsync(int? id, CancellationToken cancellationToken = default)
	{
		if (!id.HasValue)
		{
			return new CountryEditModel();
		}

		var entity = await _dbContext.Countries
			.AsNoTracking()
			.FirstOrDefaultAsync(country => country.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested country was not found.");

		return new CountryEditModel
		{
			Id = entity.Id,
			Name = entity.Name,
			Iso2Code = entity.Iso2Code,
			Iso3Code = entity.Iso3Code
		};
	}

	public async Task<int> SaveCountryAsync(CountryEditModel model, CancellationToken cancellationToken = default)
	{
		var existingByCode = await _dbContext.Countries
			.FirstOrDefaultAsync(country =>
				country.Iso2Code == model.Iso2Code.Trim().ToUpperInvariant()
				&& (!model.Id.HasValue || country.Id != model.Id.Value), cancellationToken);
		if (existingByCode is not null)
		{
			throw new InvalidOperationException("A country with the same ISO2 code already exists.");
		}

		Country entity;
		if (model.Id.HasValue)
		{
			entity = await _dbContext.Countries.FirstOrDefaultAsync(country => country.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested country was not found.");
			entity.Update(model.Name, model.Iso2Code, model.Iso3Code);
		}
		else
		{
			entity = new Country(model.Name, model.Iso2Code, model.Iso3Code);
			_dbContext.Countries.Add(entity);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	public async Task<GeographyImportResultModel> ImportCountriesAsync(CountryImportModel model, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(model.CsvContent))
		{
			throw new InvalidOperationException("Upload or paste a CSV file before importing.");
		}

		var rows = ParseCountryCsv(model.CsvContent);
		if (rows.Count == 0)
		{
			throw new InvalidOperationException("No country rows were found in the uploaded CSV.");
		}

		var existingLookup = await _dbContext.Countries
			.ToDictionaryAsync(country => country.Iso2Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

		foreach (var row in rows)
		{
			if (existingLookup.TryGetValue(row.Iso2Code, out var existing))
			{
				existing.Update(row.Name, row.Iso2Code, row.Iso3Code);
				continue;
			}

			var entity = new Country(row.Name, row.Iso2Code, row.Iso3Code);
			_dbContext.Countries.Add(entity);
			existingLookup[row.Iso2Code] = entity;
		}

		await _dbContext.SaveChangesAsync(cancellationToken);

		return new GeographyImportResultModel
		{
			ImportedRowCount = rows.Count
		};
	}

	public async Task<IReadOnlyList<StateProvinceListItem>> GetStateProvincesAsync(int? countryId = null, string? search = null, CancellationToken cancellationToken = default)
	{
		var countyCounts = await _dbContext.Counties
			.AsNoTracking()
			.GroupBy(county => county.StateProvinceId)
			.ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

		var query = _dbContext.StateProvinces
			.AsNoTracking()
			.Include(stateProvince => stateProvince.Country)
			.AsQueryable();

		if (countryId.HasValue)
		{
			query = query.Where(stateProvince => stateProvince.CountryId == countryId.Value);
		}

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim().ToUpperInvariant();
			query = query.Where(stateProvince =>
				stateProvince.Country.Name.ToUpper().Contains(term) ||
				stateProvince.Country.Iso2Code.ToUpper().Contains(term) ||
				(stateProvince.Country.Iso3Code != null && stateProvince.Country.Iso3Code.ToUpper().Contains(term)) ||
				stateProvince.Name.ToUpper().Contains(term) ||
				stateProvince.Code.ToUpper().Contains(term) ||
				stateProvince.SubdivisionType.ToUpper().Contains(term));
		}

		var states = await query
			.OrderBy(stateProvince => stateProvince.Country.Name)
			.ThenBy(stateProvince => stateProvince.Name)
			.Select(stateProvince => new StateProvinceListItem
			{
				Id = stateProvince.Id,
				CountryId = stateProvince.CountryId,
				CountryName = stateProvince.Country.Name,
				Name = stateProvince.Name,
				Code = stateProvince.Code,
				SubdivisionType = stateProvince.SubdivisionType
			})
			.ToListAsync(cancellationToken);

		var countryNameFilter = countryId.HasValue
			? states.FirstOrDefault()?.CountryName
			: null;
		foreach (var state in states)
		{
			state.CountyCount = countyCounts.GetValueOrDefault(state.Id);
			state.CountryFilterName = countryNameFilter;
		}

		return states;
	}

	public async Task<StateProvinceEditModel> GetStateProvinceAsync(int? id, int? countryId, CancellationToken cancellationToken = default)
	{
		var countryOptions = await GetCountrySelectOptionsAsync(cancellationToken);

		if (!id.HasValue)
		{
			return new StateProvinceEditModel
			{
				CountryId = countryId ?? countryOptions.Select(option => TryParseInt(option.Value)).FirstOrDefault(value => value > 0),
				SubdivisionType = "State",
				CountryOptions = countryOptions
			};
		}

		var entity = await _dbContext.StateProvinces
			.AsNoTracking()
			.FirstOrDefaultAsync(stateProvince => stateProvince.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested state or territory was not found.");

		return new StateProvinceEditModel
		{
			Id = entity.Id,
			CountryId = entity.CountryId,
			Name = entity.Name,
			Code = entity.Code,
			SubdivisionType = entity.SubdivisionType,
			CountryOptions = countryOptions
		};
	}

	public async Task<int> SaveStateProvinceAsync(StateProvinceEditModel model, CancellationToken cancellationToken = default)
	{
		await EnsureCountryExistsAsync(model.CountryId, cancellationToken);

		var existingByCode = await _dbContext.StateProvinces
			.FirstOrDefaultAsync(stateProvince =>
				stateProvince.CountryId == model.CountryId
				&& stateProvince.Code == model.Code.Trim().ToUpperInvariant()
				&& (!model.Id.HasValue || stateProvince.Id != model.Id.Value), cancellationToken);
		if (existingByCode is not null)
		{
			throw new InvalidOperationException("A state or territory with the same code already exists for that country.");
		}

		StateProvince entity;
		if (model.Id.HasValue)
		{
			entity = await _dbContext.StateProvinces.FirstOrDefaultAsync(stateProvince => stateProvince.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested state or territory was not found.");
			entity.Update(model.CountryId, model.Name, model.Code, model.SubdivisionType);
		}
		else
		{
			entity = new StateProvince(model.CountryId, model.Name, model.Code, model.SubdivisionType);
			_dbContext.StateProvinces.Add(entity);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	public async Task<GeographyImportResultModel> ImportStateProvincesAsync(StateProvinceImportModel model, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(model.CsvContent))
		{
			throw new InvalidOperationException("Upload or paste a CSV file before importing.");
		}

		var rows = ParseStateProvinceCsv(model.CsvContent);
		if (rows.Count == 0)
		{
			throw new InvalidOperationException("No state or territory rows were found in the uploaded CSV.");
		}

		var countryLookup = await _dbContext.Countries
			.AsNoTracking()
			.ToDictionaryAsync(country => country.Iso2Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
		var existingLookup = await _dbContext.StateProvinces
			.ToListAsync(cancellationToken);

		foreach (var row in rows)
		{
			if (!countryLookup.TryGetValue(row.CountryIso2Code, out var country))
			{
				throw new InvalidOperationException($"The country code '{row.CountryIso2Code}' does not exist.");
			}

			var existing = existingLookup.FirstOrDefault(stateProvince =>
				stateProvince.CountryId == country.Id
				&& string.Equals(stateProvince.Code, row.Code, StringComparison.OrdinalIgnoreCase));
			if (existing is not null)
			{
				existing.Update(country.Id, row.Name, row.Code, row.SubdivisionType);
				continue;
			}

			var entity = new StateProvince(country.Id, row.Name, row.Code, row.SubdivisionType);
			_dbContext.StateProvinces.Add(entity);
			existingLookup.Add(entity);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);

		return new GeographyImportResultModel
		{
			ImportedRowCount = rows.Count
		};
	}

	public async Task<IReadOnlyList<CountyListItem>> GetCountiesAsync(int? stateProvinceId = null, string? search = null, CancellationToken cancellationToken = default)
	{
		var query = _dbContext.Counties
			.AsNoTracking()
			.Include(county => county.StateProvince)
				.ThenInclude(stateProvince => stateProvince.Country)
			.AsQueryable();

		if (stateProvinceId.HasValue)
		{
			query = query.Where(county => county.StateProvinceId == stateProvinceId.Value);
		}

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim().ToUpperInvariant();
			query = query.Where(county =>
				county.StateProvince.Country.Name.ToUpper().Contains(term) ||
				county.StateProvince.Country.Iso2Code.ToUpper().Contains(term) ||
				(county.StateProvince.Country.Iso3Code != null && county.StateProvince.Country.Iso3Code.ToUpper().Contains(term)) ||
				county.StateProvince.Name.ToUpper().Contains(term) ||
				county.StateProvince.Code.ToUpper().Contains(term) ||
				county.Name.ToUpper().Contains(term) ||
				county.FipsCode.ToUpper().Contains(term));
		}

		var areaCounts = await _dbContext.AreaCounties
			.AsNoTracking()
			.GroupBy(areaCounty => areaCounty.CountyFips)
			.ToDictionaryAsync(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase, cancellationToken);
		var addressCounts = await _dbContext.PostalAddresses
			.AsNoTracking()
			.Where(address => address.CountyId.HasValue)
			.GroupBy(address => address.CountyId!.Value)
			.ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

		var counties = await query
			.OrderBy(county => county.StateProvince.Country.Name)
			.ThenBy(county => county.StateProvince.Name)
			.ThenBy(county => county.Name)
			.Select(county => new CountyListItem
			{
				Id = county.Id,
				StateProvinceId = county.StateProvinceId,
				StateProvinceName = county.StateProvince.Name,
				StateProvinceCode = county.StateProvince.Code,
				CountryName = county.StateProvince.Country.Name,
				Name = county.Name,
				FipsCode = county.FipsCode
			})
			.ToListAsync(cancellationToken);

		foreach (var county in counties)
		{
			county.AddressCount = addressCounts.GetValueOrDefault(county.Id);
			county.AreaCount = areaCounts.GetValueOrDefault(county.FipsCode);
		}

		var stateProvinceFilterName = stateProvinceId.HasValue
			? counties.FirstOrDefault() is { } firstCounty
				? $"{firstCounty.StateProvinceName} ({firstCounty.StateProvinceCode})"
				: null
			: null;
		foreach (var county in counties)
		{
			county.StateProvinceFilterName = stateProvinceFilterName;
		}

		return counties;
	}

	public async Task<CountyEditModel> GetCountyAsync(int? id, int? stateProvinceId, CancellationToken cancellationToken = default)
	{
		var stateProvinceOptions = await GetStateProvinceSelectOptionsAsync(null, cancellationToken);

		if (!id.HasValue)
		{
			return new CountyEditModel
			{
				StateProvinceId = stateProvinceId ?? stateProvinceOptions.Select(option => TryParseInt(option.Value)).FirstOrDefault(value => value > 0),
				StateProvinceOptions = stateProvinceOptions
			};
		}

		var entity = await _dbContext.Counties
			.AsNoTracking()
			.FirstOrDefaultAsync(county => county.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested county was not found.");

		return new CountyEditModel
		{
			Id = entity.Id,
			StateProvinceId = entity.StateProvinceId,
			Name = entity.Name,
			FipsCode = entity.FipsCode,
			StateProvinceOptions = stateProvinceOptions
		};
	}

	public async Task<int> SaveCountyAsync(CountyEditModel model, CancellationToken cancellationToken = default)
	{
		await EnsureStateProvinceExistsAsync(model.StateProvinceId, cancellationToken);

		var existingByFips = await _dbContext.Counties
			.FirstOrDefaultAsync(county =>
				county.StateProvinceId == model.StateProvinceId
				&& county.FipsCode == model.FipsCode.Trim().ToUpperInvariant()
				&& (!model.Id.HasValue || county.Id != model.Id.Value), cancellationToken);
		if (existingByFips is not null)
		{
			throw new InvalidOperationException("A county with the same FIPS code already exists for that state or territory.");
		}

		County entity;
		if (model.Id.HasValue)
		{
			entity = await _dbContext.Counties.FirstOrDefaultAsync(county => county.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested county was not found.");
			entity.Update(model.StateProvinceId, model.Name, model.FipsCode);
		}
		else
		{
			entity = new County(model.StateProvinceId, model.Name, model.FipsCode);
			_dbContext.Counties.Add(entity);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	public async Task<GeographyImportResultModel> ImportCountiesAsync(CountyImportModel model, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(model.CsvContent))
		{
			throw new InvalidOperationException("Upload or paste a CSV file before importing.");
		}

		var rows = ParseCountyCsv(model.CsvContent);
		if (rows.Count == 0)
		{
			throw new InvalidOperationException("No county rows were found in the uploaded CSV.");
		}

		var states = await _dbContext.StateProvinces
			.AsNoTracking()
			.Include(stateProvince => stateProvince.Country)
			.ToListAsync(cancellationToken);
		var existingLookup = await _dbContext.Counties.ToListAsync(cancellationToken);

		foreach (var row in rows)
		{
			var stateProvince = states.FirstOrDefault(entity =>
				string.Equals(entity.Code, row.StateProvinceCode, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(entity.Country.Iso2Code, row.CountryIso2Code, StringComparison.OrdinalIgnoreCase))
				?? throw new InvalidOperationException($"The state code '{row.StateProvinceCode}' for country '{row.CountryIso2Code}' does not exist.");

			var existing = existingLookup.FirstOrDefault(county =>
				county.StateProvinceId == stateProvince.Id
				&& string.Equals(county.FipsCode, row.FipsCode, StringComparison.OrdinalIgnoreCase));
			if (existing is not null)
			{
				existing.Update(stateProvince.Id, row.Name, row.FipsCode);
				continue;
			}

			var entity = new County(stateProvince.Id, row.Name, row.FipsCode);
			_dbContext.Counties.Add(entity);
			existingLookup.Add(entity);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);

		return new GeographyImportResultModel
		{
			ImportedRowCount = rows.Count
		};
	}

	public async Task<IReadOnlyList<PostalAddressListItem>> GetPostalAddressesAsync(string? search = null, int? countyId = null, CancellationToken cancellationToken = default)
	{
		var query = _dbContext.PostalAddresses
			.AsNoTracking()
			.Include(address => address.Country)
			.Include(address => address.StateProvince)
			.Include(address => address.County)
			.Include(address => address.People)
			.Include(address => address.SurveyResponses)
			.AsQueryable();

		if (countyId.HasValue)
		{
			query = query.Where(address => address.CountyId == countyId.Value);
		}

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim().ToUpperInvariant();
			query = query.Where(address =>
				address.AddressLine1.ToUpper().Contains(term) ||
				(address.AddressLine2 != null && address.AddressLine2.ToUpper().Contains(term)) ||
				address.City.ToUpper().Contains(term) ||
				address.PostalCode.ToUpper().Contains(term) ||
				address.FormattedAddress.ToUpper().Contains(term));
		}

		return await query
			.OrderBy(address => address.Country.Name)
			.ThenBy(address => address.StateProvince != null ? address.StateProvince.Code : string.Empty)
			.ThenBy(address => address.City)
			.ThenBy(address => address.AddressLine1)
			.Select(address => new PostalAddressListItem
			{
				Id = address.Id,
				CountyId = address.CountyId,
				CountyName = address.County != null ? address.County.Name : null,
				AddressLine1 = address.AddressLine1,
				AddressLine2 = address.AddressLine2,
				City = address.City,
				StateProvinceCode = address.StateProvince != null ? address.StateProvince.Code : string.Empty,
				CountryCode = address.Country.Iso2Code,
				PostalCode = address.PostalCode,
				FormattedAddress = address.FormattedAddress,
				ReferenceCount = address.People.Count + address.SurveyResponses.Count
			})
			.Take(250)
			.ToListAsync(cancellationToken);
	}

	public async Task<PostalAddressEditModel> GetPostalAddressAsync(int? id, CancellationToken cancellationToken = default)
	{
		var countryOptions = await GetCountrySelectOptionsAsync(cancellationToken);

		if (!id.HasValue)
		{
			return new PostalAddressEditModel
			{
				CountryId = 0,
				StateProvinceId = 0,
				CountryOptions = countryOptions,
				StateProvinceOptions = Array.Empty<SelectOption>(),
				CountyOptions = Array.Empty<SelectOption>()
			};
		}

		var entity = await _dbContext.PostalAddresses
			.AsNoTracking()
			.Include(address => address.People)
			.Include(address => address.SurveyResponses)
			.FirstOrDefaultAsync(address => address.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested address was not found.");

		var stateProvinceOptions = await GetStateProvinceSelectOptionsAsync(entity.CountryId, cancellationToken);
		var countyOptions = await GetCountySelectOptionsAsync(entity.StateProvinceId, cancellationToken);

		return new PostalAddressEditModel
		{
			Id = entity.Id,
			CountryId = entity.CountryId,
			StateProvinceId = entity.StateProvinceId ?? 0,
			CountyId = entity.CountyId,
			AddressLine1 = entity.AddressLine1,
			AddressLine2 = entity.AddressLine2,
			City = entity.City,
			PostalCode = entity.PostalCode,
			FormattedAddress = entity.FormattedAddress,
			ReferenceCount = entity.People.Count + entity.SurveyResponses.Count,
			CountryOptions = countryOptions,
			StateProvinceOptions = stateProvinceOptions,
			CountyOptions = countyOptions
		};
	}

	public async Task<PostalAddressReferenceViewModel> GetPostalAddressReferencesAsync(int id, CancellationToken cancellationToken = default)
	{
		var address = await _dbContext.PostalAddresses
			.AsNoTracking()
			.Include(entity => entity.Country)
			.Include(entity => entity.County)
			.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
			?? throw new InvalidOperationException("The requested address was not found.");

		var peopleData = await _dbContext.People
			.AsNoTracking()
			.Where(person => person.PostalAddressId == id)
			.OrderBy(person => person.LastName)
			.ThenBy(person => person.FirstName)
			.ThenBy(person => person.MiddleName)
			.Select(person => new
			{
				Id = person.Id,
				person.FirstName,
				person.MiddleName,
				person.LastName,
				Email = person.Email,
				PhoneNumber = person.PhoneNumber
			})
			.ToListAsync(cancellationToken);
		var people = peopleData
			.Select(person => new PostalAddressPersonReferenceItem
			{
				Id = person.Id,
				FullName = string.Join(" ", new[] { person.FirstName, person.MiddleName, person.LastName }.Where(part => !string.IsNullOrWhiteSpace(part))),
				Email = person.Email,
				PhoneNumber = person.PhoneNumber
			})
			.ToList();

		var responseData = await _dbContext.SurveyResponses
			.AsNoTracking()
			.Where(response => response.RespondentPostalAddressId == id)
			.Select(response => new
			{
				Id = response.Id,
				response.RespondentFirstName,
				response.RespondentMiddleName,
				response.RespondentLastName,
				SurveyName = response.SurveyNameSnapshot,
				VersionName = response.SurveyVersionNameSnapshot,
				SubmittedUtc = response.SubmittedUtc
			})
			.ToListAsync(cancellationToken);
		var responses = responseData
			.Select(response => new PostalAddressResponseReferenceItem
			{
				Id = response.Id,
				RespondentName = string.Join(" ", new[] { response.RespondentFirstName, response.RespondentMiddleName, response.RespondentLastName }.Where(part => !string.IsNullOrWhiteSpace(part))),
				SurveyName = response.SurveyName,
				VersionName = response.VersionName,
				SubmittedUtc = response.SubmittedUtc
			})
			.OrderByDescending(response => response.SubmittedUtc)
			.ThenBy(response => response.RespondentName)
			.ToList();

		return new PostalAddressReferenceViewModel
		{
			Id = address.Id,
			FormattedAddress = address.FormattedAddress,
			CountryCode = address.Country.Iso2Code,
			CountyName = address.County?.Name,
			People = people,
			Responses = responses
		};
	}

	public async Task<int> SavePostalAddressAsync(PostalAddressEditModel model, CancellationToken cancellationToken = default)
	{
		var country = await _dbContext.Countries
			.AsNoTracking()
			.FirstOrDefaultAsync(entity => entity.Id == model.CountryId, cancellationToken)
			?? throw new InvalidOperationException("The selected country was not found.");
		var stateProvince = await _dbContext.StateProvinces
			.AsNoTracking()
			.FirstOrDefaultAsync(entity => entity.Id == model.StateProvinceId && entity.CountryId == model.CountryId, cancellationToken)
			?? throw new InvalidOperationException("The selected state or territory was not found.");
		County? county = null;
		if (model.CountyId.HasValue)
		{
			county = await _dbContext.Counties
				.AsNoTracking()
				.FirstOrDefaultAsync(entity => entity.Id == model.CountyId.Value && entity.StateProvinceId == model.StateProvinceId, cancellationToken)
				?? throw new InvalidOperationException("The selected county was not found.");
		}

		var normalizedPostalCode = PostalCodeNormalizer.Normalize(model.PostalCode, nameof(model.PostalCode))
			?? throw new InvalidOperationException("A postal code is required.");
		var normalizedKey = PostalAddressKeyBuilder.Build(country.Iso2Code, stateProvince.Code, model.AddressLine1, model.AddressLine2, model.City, normalizedPostalCode);
		var duplicate = await _dbContext.PostalAddresses
			.FirstOrDefaultAsync(address => address.NormalizedKey == normalizedKey && (!model.Id.HasValue || address.Id != model.Id.Value), cancellationToken);
		if (duplicate is not null)
		{
			throw new InvalidOperationException("That address already exists.");
		}

		PostalAddress entity;
		if (model.Id.HasValue)
		{
			entity = await _dbContext.PostalAddresses.FirstOrDefaultAsync(address => address.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested address was not found.");
			entity.Update(country.Id, stateProvince.Id, county?.Id, model.AddressLine1, model.AddressLine2, model.City, normalizedPostalCode, country.Iso2Code, stateProvince.Code, country.Name);
		}
		else
		{
			entity = new PostalAddress(country.Id, stateProvince.Id, county?.Id, model.AddressLine1, model.AddressLine2, model.City, normalizedPostalCode, country.Iso2Code, stateProvince.Code, country.Name);
			_dbContext.PostalAddresses.Add(entity);
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	public async Task<GeographyImportResultModel> ImportPostalAddressesAsync(PostalAddressImportModel model, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(model.CsvContent))
		{
			throw new InvalidOperationException("Upload or paste a CSV file before importing.");
		}

		var rows = ParsePostalAddressCsv(model.CsvContent);
		if (rows.Count == 0)
		{
			throw new InvalidOperationException("No address rows were found in the uploaded CSV.");
		}

		foreach (var row in rows)
		{
			var country = await _dbContext.Countries
				.AsNoTracking()
				.FirstOrDefaultAsync(entity => entity.Iso2Code == row.CountryIso2Code, cancellationToken)
				?? throw new InvalidOperationException($"The country code '{row.CountryIso2Code}' does not exist.");
			var stateProvince = await _dbContext.StateProvinces
				.AsNoTracking()
				.FirstOrDefaultAsync(entity => entity.CountryId == country.Id && entity.Code == row.StateProvinceCode, cancellationToken)
				?? throw new InvalidOperationException($"The state code '{row.StateProvinceCode}' does not exist for '{row.CountryIso2Code}'.");
			County? county = null;
			if (!string.IsNullOrWhiteSpace(row.CountyFipsCode))
			{
				county = await _dbContext.Counties
					.AsNoTracking()
					.FirstOrDefaultAsync(entity => entity.StateProvinceId == stateProvince.Id && entity.FipsCode == row.CountyFipsCode, cancellationToken)
					?? throw new InvalidOperationException($"The county code '{row.CountyFipsCode}' does not exist for '{row.StateProvinceCode}'.");
			}

			await ResolveOrCreatePostalAddressAsync(
				country,
				stateProvince,
				county,
				row.AddressLine1,
				row.AddressLine2,
				row.City,
				row.PostalCode,
				cancellationToken);
		}

		return new GeographyImportResultModel
		{
			ImportedRowCount = rows.Count
		};
	}

	private async Task<IReadOnlyList<SelectOption>> GetCountrySelectOptionsAsync(CancellationToken cancellationToken)
	{
		return await _dbContext.Countries
			.AsNoTracking()
			.OrderBy(country => country.Name)
			.Select(country => new SelectOption
			{
				Value = country.Id.ToString(),
				Label = $"{country.Name} ({country.Iso2Code})"
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<SelectOption>> GetStateProvinceSelectOptionsAsync(int? countryId, CancellationToken cancellationToken = default)
	{
		var query = _dbContext.StateProvinces
			.AsNoTracking()
			.Include(stateProvince => stateProvince.Country)
			.AsQueryable();

		if (countryId.HasValue && countryId.Value > 0)
		{
			query = query.Where(stateProvince => stateProvince.CountryId == countryId.Value);
		}

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

	public async Task<IReadOnlyList<SelectOption>> GetCountySelectOptionsAsync(int? stateProvinceId, CancellationToken cancellationToken = default)
	{
		var query = _dbContext.Counties
			.AsNoTracking()
			.Include(county => county.StateProvince)
			.AsQueryable();

		if (stateProvinceId.HasValue && stateProvinceId.Value > 0)
		{
			query = query.Where(county => county.StateProvinceId == stateProvinceId.Value);
		}

		return await query
			.OrderBy(county => county.StateProvince.Code)
			.ThenBy(county => county.Name)
			.Select(county => new SelectOption
			{
				Value = county.Id.ToString(),
				Label = $"{county.StateProvince.Code} - {county.Name} ({county.FipsCode})"
			})
			.ToListAsync(cancellationToken);
	}

	private async Task EnsureCountryExistsAsync(int countryId, CancellationToken cancellationToken)
	{
		var exists = await _dbContext.Countries.AnyAsync(country => country.Id == countryId, cancellationToken);
		if (!exists)
		{
			throw new InvalidOperationException("The selected country was not found.");
		}
	}

	private async Task EnsureStateProvinceExistsAsync(int stateProvinceId, CancellationToken cancellationToken)
	{
		var exists = await _dbContext.StateProvinces.AnyAsync(stateProvince => stateProvince.Id == stateProvinceId, cancellationToken);
		if (!exists)
		{
			throw new InvalidOperationException("The selected state or territory was not found.");
		}
	}

	private async Task<int> GetDefaultCountryIdAsync(CancellationToken cancellationToken)
	{
		var unitedStatesId = await _dbContext.Countries
			.AsNoTracking()
			.Where(country => country.Iso2Code == "US")
			.Select(country => (int?)country.Id)
			.FirstOrDefaultAsync(cancellationToken);
		if (unitedStatesId.HasValue)
		{
			return unitedStatesId.Value;
		}

		return await _dbContext.Countries
			.AsNoTracking()
			.OrderBy(country => country.Name)
			.Select(country => country.Id)
			.FirstOrDefaultAsync(cancellationToken);
	}

	private async Task<int?> TryResolveLegacyStateProvinceIdAsync(string? stateValue, int? countryId, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(stateValue))
		{
			return null;
		}

		var normalized = stateValue.Trim();
		return await _dbContext.StateProvinces
			.AsNoTracking()
			.Where(stateProvince => !countryId.HasValue || stateProvince.CountryId == countryId.Value)
			.Where(stateProvince => stateProvince.Code == normalized || stateProvince.Name == normalized)
			.Select(stateProvince => (int?)stateProvince.Id)
			.FirstOrDefaultAsync(cancellationToken);
	}

	private async Task<ResolvedPostalAddress> ResolveOrCreatePostalAddressAsync(
		int countryId,
		int stateProvinceId,
		int? countyId,
		string addressLine1,
		string? addressLine2,
		string city,
		string? postalCode,
		CancellationToken cancellationToken)
	{
		var country = await _dbContext.Countries
			.AsNoTracking()
			.FirstOrDefaultAsync(entity => entity.Id == countryId, cancellationToken)
			?? throw new InvalidOperationException("The selected country was not found.");
		var stateProvince = await _dbContext.StateProvinces
			.AsNoTracking()
			.FirstOrDefaultAsync(entity => entity.Id == stateProvinceId && entity.CountryId == countryId, cancellationToken)
			?? throw new InvalidOperationException("The selected state or territory was not found.");
		var county = await ResolveCountyAsync(stateProvince, countyId, postalCode, cancellationToken);

		return await ResolveOrCreatePostalAddressAsync(
			country,
			stateProvince,
			county,
			addressLine1,
			addressLine2,
			city,
			postalCode,
			cancellationToken);
	}

	private async Task<ResolvedPostalAddress> ResolveOrCreatePostalAddressAsync(
		Country country,
		StateProvince stateProvince,
		County? county,
		string addressLine1,
		string? addressLine2,
		string city,
		string? postalCode,
		CancellationToken cancellationToken)
	{
		var normalizedPostalCode = PostalCodeNormalizer.Normalize(postalCode, nameof(postalCode))
			?? throw new InvalidOperationException("A postal code is required.");
		var normalizedKey = PostalAddressKeyBuilder.Build(country.Iso2Code, stateProvince.Code, addressLine1, addressLine2, city, normalizedPostalCode);
		var address = await _dbContext.PostalAddresses
			.FirstOrDefaultAsync(entity => entity.NormalizedKey == normalizedKey, cancellationToken);

		if (address is null)
		{
			address = new PostalAddress(
				country.Id,
				stateProvince.Id,
				county?.Id,
				addressLine1,
				addressLine2,
				city,
				normalizedPostalCode,
				country.Iso2Code,
				stateProvince.Code,
				country.Name);
			_dbContext.PostalAddresses.Add(address);
			await _dbContext.SaveChangesAsync(cancellationToken);
		}
		else if (address.CountryId != country.Id || address.StateProvinceId != stateProvince.Id || address.CountyId != county?.Id)
		{
			address.Update(
				country.Id,
				stateProvince.Id,
				county?.Id,
				addressLine1,
				addressLine2,
				city,
				normalizedPostalCode,
				country.Iso2Code,
				stateProvince.Code,
				country.Name);
			await _dbContext.SaveChangesAsync(cancellationToken);
		}

		return new ResolvedPostalAddress(address, country, stateProvince, county);
	}

	private async Task<County?> ResolveCountyAsync(StateProvince stateProvince, int? countyId, string? postalCode, CancellationToken cancellationToken)
	{
		if (countyId.HasValue && countyId.Value > 0)
		{
			var county = await _dbContext.Counties
				.AsNoTracking()
				.FirstOrDefaultAsync(entity => entity.Id == countyId.Value && entity.StateProvinceId == stateProvince.Id, cancellationToken)
				?? throw new InvalidOperationException("The selected county was not found.");

			await ValidateCountyMatchesPostalCodeAsync(stateProvince, county, postalCode, cancellationToken);
			return county;
		}

		return await ResolveCountyFromPostalCodeAsync(stateProvince, postalCode, cancellationToken);
	}

	private async Task<County?> ResolveCountyFromPostalCodeAsync(StateProvince stateProvince, string? postalCode, CancellationToken cancellationToken)
	{
		var normalizedPostalCode = PostalCodeNormalizer.Normalize(postalCode, nameof(postalCode));
		if (string.IsNullOrWhiteSpace(normalizedPostalCode))
		{
			return null;
		}

		var mappings = await _dbContext.ZipCountyLookups
			.AsNoTracking()
			.Where(mapping => mapping.ZipCode == normalizedPostalCode && mapping.StateCode == stateProvince.Code)
			.Select(mapping => new
			{
				mapping.CountyFips,
				mapping.CountyName,
				mapping.ResidentialRatio
			})
			.ToListAsync(cancellationToken);
		var countyFips = mappings
			.OrderByDescending(mapping => mapping.ResidentialRatio)
			.ThenBy(mapping => mapping.CountyName)
			.Select(mapping => mapping.CountyFips)
			.FirstOrDefault();
		if (string.IsNullOrWhiteSpace(countyFips))
		{
			return null;
		}

		return await _dbContext.Counties
			.AsNoTracking()
			.FirstOrDefaultAsync(county => county.StateProvinceId == stateProvince.Id && county.FipsCode == countyFips, cancellationToken);
	}

	private async Task ValidateCountyMatchesPostalCodeAsync(StateProvince stateProvince, County county, string? postalCode, CancellationToken cancellationToken)
	{
		var normalizedPostalCode = PostalCodeNormalizer.Normalize(postalCode, nameof(postalCode));
		if (string.IsNullOrWhiteSpace(normalizedPostalCode))
		{
			return;
		}

		var matchingCountyFips = await _dbContext.ZipCountyLookups
			.AsNoTracking()
			.Where(mapping => mapping.ZipCode == normalizedPostalCode && mapping.StateCode == stateProvince.Code)
			.Select(mapping => mapping.CountyFips)
			.Distinct()
			.ToListAsync(cancellationToken);
		if (matchingCountyFips.Count == 0)
		{
			return;
		}

		if (!matchingCountyFips.Contains(county.FipsCode, StringComparer.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("The selected county does not match the postal code.");
		}
	}

	private async Task<PersonEditModel> BuildPersonEditModelAsync(Person? entity, CancellationToken cancellationToken)
	{
		var countryId = entity?.PostalAddress?.CountryId ?? 0;
		var stateProvinceId = entity?.PostalAddress?.StateProvinceId
			?? (countryId > 0 ? await TryResolveLegacyStateProvinceIdAsync(entity?.State, countryId, cancellationToken) : null)
			?? 0;
		var countryOptions = await GetCountrySelectOptionsAsync(cancellationToken);
		var stateProvinceOptions = countryId > 0
			? await GetStateProvinceSelectOptionsAsync(countryId, cancellationToken)
			: Array.Empty<SelectOption>();
		var countyOptions = stateProvinceId > 0
			? await GetCountySelectOptionsAsync(stateProvinceId, cancellationToken)
			: Array.Empty<SelectOption>();

		return new PersonEditModel
		{
			Id = entity?.Id,
			FirstName = entity?.FirstName ?? string.Empty,
			MiddleName = entity?.MiddleName,
			LastName = entity?.LastName ?? string.Empty,
			AddressLine1 = entity?.AddressLine1 ?? entity?.PostalAddress?.AddressLine1 ?? entity?.HomeAddress ?? string.Empty,
			AddressLine2 = entity?.AddressLine2 ?? entity?.PostalAddress?.AddressLine2,
			City = entity?.City ?? entity?.PostalAddress?.City ?? string.Empty,
			CountryId = countryId,
			StateProvinceId = stateProvinceId,
			CountyId = entity?.PostalAddress?.CountyId,
			PostalCode = entity?.PostalCode ?? entity?.PostalAddress?.PostalCode ?? PostalCodeNormalizer.Extract(entity?.HomeAddress),
			PhoneNumber = entity?.PhoneNumber ?? string.Empty,
			BestTimeToContact = entity?.BestTimeToContact,
			Email = entity?.Email ?? string.Empty,
			CountryOptions = countryOptions,
			StateProvinceOptions = stateProvinceOptions,
			CountyOptions = countyOptions
		};
	}

	private async Task<RespondentContactModel> BuildRespondentContactModelAsync(Person person, CancellationToken cancellationToken)
	{
		var countryId = person.PostalAddress?.CountryId ?? 0;
		var stateProvinceId = person.PostalAddress?.StateProvinceId
			?? (countryId > 0 ? await TryResolveLegacyStateProvinceIdAsync(person.State, countryId, cancellationToken) : null)
			?? 0;
		var countryOptions = await GetCountrySelectOptionsAsync(cancellationToken);
		var stateProvinceOptions = countryId > 0
			? await GetStateProvinceSelectOptionsAsync(countryId, cancellationToken)
			: Array.Empty<SelectOption>();
		var countyOptions = stateProvinceId > 0
			? await GetCountySelectOptionsAsync(stateProvinceId, cancellationToken)
			: Array.Empty<SelectOption>();

		return new RespondentContactModel
		{
			FirstName = person.FirstName,
			MiddleName = person.MiddleName,
			LastName = person.LastName,
			AddressLine1 = person.AddressLine1 ?? person.PostalAddress?.AddressLine1 ?? person.HomeAddress,
			AddressLine2 = person.AddressLine2 ?? person.PostalAddress?.AddressLine2,
			City = person.City ?? person.PostalAddress?.City ?? string.Empty,
			CountryId = countryId,
			StateProvinceId = stateProvinceId,
			CountyId = person.PostalAddress?.CountyId,
			PostalCode = person.PostalCode ?? person.PostalAddress?.PostalCode ?? PostalCodeNormalizer.Extract(person.HomeAddress),
			PhoneNumber = person.PhoneNumber,
			BestTimeToContact = person.BestTimeToContact,
			Email = person.Email,
			CountryOptions = countryOptions,
			StateProvinceOptions = stateProvinceOptions,
			CountyOptions = countyOptions
		};
	}

	private static List<ImportedCountryRow> ParseCountryCsv(string csvContent)
	{
		using var reader = new StringReader(csvContent);
		using var parser = CreateCsvParser(reader);

		if (parser.EndOfData)
		{
			return [];
		}

		var headerLookup = CreateHeaderLookup(parser.ReadFields() ?? []);
		var nameIndex = FindRequiredCsvHeader(headerLookup, "NAME", "COUNTRY", "COUNTRYNAME");
		var iso2Index = FindRequiredCsvHeader(headerLookup, "ISO2", "ISO_2", "COUNTRYCODE", "COUNTRY_CODE", "CODE");
		var iso3Index = FindOptionalCsvHeader(headerLookup, "ISO3", "ISO_3", "THREELETTERCODE", "THREE_LETTER_CODE");

		var rows = new List<ImportedCountryRow>();
		while (!parser.EndOfData)
		{
			var fields = parser.ReadFields();
			if (fields is null || fields.All(string.IsNullOrWhiteSpace))
			{
				continue;
			}

			var name = GetCsvField(fields, nameIndex);
			var iso2Code = GetCsvField(fields, iso2Index);
			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(iso2Code))
			{
				continue;
			}

			rows.Add(new ImportedCountryRow(name.Trim(), iso2Code.Trim().ToUpperInvariant(), GetCsvField(fields, iso3Index)?.Trim().ToUpperInvariant()));
		}

		return rows
			.GroupBy(row => row.Iso2Code, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.Last())
			.ToList();
	}

	private static List<ImportedStateProvinceRow> ParseStateProvinceCsv(string csvContent)
	{
		using var reader = new StringReader(csvContent);
		using var parser = CreateCsvParser(reader);

		if (parser.EndOfData)
		{
			return [];
		}

		var headerLookup = CreateHeaderLookup(parser.ReadFields() ?? []);
		var countryIndex = FindRequiredCsvHeader(headerLookup, "COUNTRYCODE", "COUNTRY_CODE", "COUNTRY", "ISO2");
		var nameIndex = FindRequiredCsvHeader(headerLookup, "NAME", "STATENAME", "STATE_NAME", "PROVINCENAME", "PROVINCE_NAME");
		var codeIndex = FindRequiredCsvHeader(headerLookup, "CODE", "STATECODE", "STATE_CODE", "PROVINCECODE", "PROVINCE_CODE");
		var typeIndex = FindOptionalCsvHeader(headerLookup, "TYPE", "SUBDIVISIONTYPE", "SUBDIVISION_TYPE");

		var rows = new List<ImportedStateProvinceRow>();
		while (!parser.EndOfData)
		{
			var fields = parser.ReadFields();
			if (fields is null || fields.All(string.IsNullOrWhiteSpace))
			{
				continue;
			}

			var countryIso2Code = GetCsvField(fields, countryIndex);
			var name = GetCsvField(fields, nameIndex);
			var code = GetCsvField(fields, codeIndex);
			if (string.IsNullOrWhiteSpace(countryIso2Code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
			{
				continue;
			}

			rows.Add(new ImportedStateProvinceRow(
				countryIso2Code.Trim().ToUpperInvariant(),
				name.Trim(),
				code.Trim().ToUpperInvariant(),
				string.IsNullOrWhiteSpace(GetCsvField(fields, typeIndex)) ? "State" : GetCsvField(fields, typeIndex)!.Trim()));
		}

		return rows
			.GroupBy(row => $"{row.CountryIso2Code}|{row.Code}", StringComparer.OrdinalIgnoreCase)
			.Select(group => group.Last())
			.ToList();
	}

	private static List<ImportedCountyRow> ParseCountyCsv(string csvContent)
	{
		using var reader = new StringReader(csvContent);
		using var parser = CreateCsvParser(reader);

		if (parser.EndOfData)
		{
			return [];
		}

		var headerLookup = CreateHeaderLookup(parser.ReadFields() ?? []);
		var countryIndex = FindRequiredCsvHeader(headerLookup, "COUNTRYCODE", "COUNTRY_CODE", "COUNTRY", "ISO2");
		var stateIndex = FindRequiredCsvHeader(headerLookup, "STATECODE", "STATE_CODE", "STATE", "PROVINCECODE", "PROVINCE_CODE");
		var nameIndex = FindRequiredCsvHeader(headerLookup, "NAME", "COUNTYNAME", "COUNTY_NAME");
		var fipsIndex = FindRequiredCsvHeader(headerLookup, "FIPS", "COUNTYFIPS", "COUNTY_FIPS", "CODE");

		var rows = new List<ImportedCountyRow>();
		while (!parser.EndOfData)
		{
			var fields = parser.ReadFields();
			if (fields is null || fields.All(string.IsNullOrWhiteSpace))
			{
				continue;
			}

			var countryIso2Code = GetCsvField(fields, countryIndex);
			var stateProvinceCode = GetCsvField(fields, stateIndex);
			var name = GetCsvField(fields, nameIndex);
			var fipsCode = GetCsvField(fields, fipsIndex);
			if (string.IsNullOrWhiteSpace(countryIso2Code) || string.IsNullOrWhiteSpace(stateProvinceCode) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(fipsCode))
			{
				continue;
			}

			rows.Add(new ImportedCountyRow(
				countryIso2Code.Trim().ToUpperInvariant(),
				stateProvinceCode.Trim().ToUpperInvariant(),
				name.Trim(),
				fipsCode.Trim().ToUpperInvariant()));
		}

		return rows
			.GroupBy(row => $"{row.CountryIso2Code}|{row.StateProvinceCode}|{row.FipsCode}", StringComparer.OrdinalIgnoreCase)
			.Select(group => group.Last())
			.ToList();
	}

	private static List<ImportedPostalAddressRow> ParsePostalAddressCsv(string csvContent)
	{
		using var reader = new StringReader(csvContent);
		using var parser = CreateCsvParser(reader);

		if (parser.EndOfData)
		{
			return [];
		}

		var headerLookup = CreateHeaderLookup(parser.ReadFields() ?? []);
		var countryIndex = FindRequiredCsvHeader(headerLookup, "COUNTRYCODE", "COUNTRY_CODE", "COUNTRY", "ISO2");
		var stateIndex = FindRequiredCsvHeader(headerLookup, "STATECODE", "STATE_CODE", "STATE", "PROVINCECODE", "PROVINCE_CODE");
		var countyIndex = FindOptionalCsvHeader(headerLookup, "COUNTYFIPS", "COUNTY_FIPS", "COUNTYCODE", "COUNTY_CODE");
		var addressLine1Index = FindRequiredCsvHeader(headerLookup, "ADDRESSLINE1", "ADDRESS_LINE_1", "ADDRESS1", "LINE1");
		var addressLine2Index = FindOptionalCsvHeader(headerLookup, "ADDRESSLINE2", "ADDRESS_LINE_2", "ADDRESS2", "LINE2");
		var cityIndex = FindRequiredCsvHeader(headerLookup, "CITY");
		var postalCodeIndex = FindRequiredCsvHeader(headerLookup, "POSTALCODE", "POSTAL_CODE", "ZIP", "ZIPCODE", "ZIP_CODE");

		var rows = new List<ImportedPostalAddressRow>();
		while (!parser.EndOfData)
		{
			var fields = parser.ReadFields();
			if (fields is null || fields.All(string.IsNullOrWhiteSpace))
			{
				continue;
			}

			var countryIso2Code = GetCsvField(fields, countryIndex);
			var stateProvinceCode = GetCsvField(fields, stateIndex);
			var addressLine1 = GetCsvField(fields, addressLine1Index);
			var city = GetCsvField(fields, cityIndex);
			var postalCode = GetCsvField(fields, postalCodeIndex);
			if (string.IsNullOrWhiteSpace(countryIso2Code)
				|| string.IsNullOrWhiteSpace(stateProvinceCode)
				|| string.IsNullOrWhiteSpace(addressLine1)
				|| string.IsNullOrWhiteSpace(city)
				|| string.IsNullOrWhiteSpace(postalCode))
			{
				continue;
			}

			rows.Add(new ImportedPostalAddressRow(
				countryIso2Code.Trim().ToUpperInvariant(),
				stateProvinceCode.Trim().ToUpperInvariant(),
				GetCsvField(fields, countyIndex)?.Trim().ToUpperInvariant(),
				addressLine1.Trim(),
				GetCsvField(fields, addressLine2Index)?.Trim(),
				city.Trim(),
				postalCode.Trim()));
		}

		return rows
			.GroupBy(row => PostalAddressKeyBuilder.Build(row.CountryIso2Code, row.StateProvinceCode, row.AddressLine1, row.AddressLine2, row.City, PostalCodeNormalizer.Normalize(row.PostalCode, nameof(row.PostalCode)) ?? row.PostalCode), StringComparer.OrdinalIgnoreCase)
			.Select(group => group.Last())
			.ToList();
	}

	private static TextFieldParser CreateCsvParser(StringReader reader)
	{
		var parser = new TextFieldParser(reader)
		{
			TextFieldType = FieldType.Delimited,
			HasFieldsEnclosedInQuotes = true,
			TrimWhiteSpace = true
		};
		parser.SetDelimiters(",");
		return parser;
	}

	private static Dictionary<string, int> CreateHeaderLookup(string[] headers)
	{
		return headers
			.Select((header, index) => new { Header = header?.Trim() ?? string.Empty, Index = index })
			.Where(item => !string.IsNullOrWhiteSpace(item.Header))
			.ToDictionary(item => item.Header, item => item.Index, StringComparer.OrdinalIgnoreCase);
	}

	private static int FindRequiredCsvHeader(IReadOnlyDictionary<string, int> headerLookup, params string[] names)
	{
		var index = FindOptionalCsvHeader(headerLookup, names);
		return index ?? throw new InvalidOperationException($"The imported CSV must include one of these columns: {string.Join(", ", names)}.");
	}

	private static int? FindOptionalCsvHeader(IReadOnlyDictionary<string, int> headerLookup, params string[] names)
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

	private static string? GetCsvField(string[] fields, int? index)
	{
		if (!index.HasValue || index.Value < 0 || index.Value >= fields.Length)
		{
			return null;
		}

		return fields[index.Value];
	}

	private sealed record ImportedCountryRow(
		string Name,
		string Iso2Code,
		string? Iso3Code);

	private sealed record ImportedStateProvinceRow(
		string CountryIso2Code,
		string Name,
		string Code,
		string SubdivisionType);

	private sealed record ImportedCountyRow(
		string CountryIso2Code,
		string StateProvinceCode,
		string Name,
		string FipsCode);

	private sealed record ImportedPostalAddressRow(
		string CountryIso2Code,
		string StateProvinceCode,
		string? CountyFipsCode,
		string AddressLine1,
		string? AddressLine2,
		string City,
		string PostalCode);

	private sealed record ResolvedPostalAddress(
		PostalAddress PostalAddress,
		Country Country,
		StateProvince StateProvince,
		County? County);
}
