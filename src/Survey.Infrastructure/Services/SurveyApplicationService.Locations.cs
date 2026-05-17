using Microsoft.EntityFrameworkCore;
using Survey.Application.Models;
using Survey.Domain;

namespace Survey.Infrastructure.Services;

public sealed partial class SurveyApplicationService
{
	public async Task<IReadOnlyList<LocationListItem>> GetLocationsAsync(int? personId = null, CancellationToken cancellationToken = default)
	{
		var query = _dbContext.Locations
			.AsNoTracking()
			.Include(location => location.Person)
			.Include(location => location.Assignments)
			.AsQueryable();

		if (personId.HasValue)
		{
			query = query.Where(location => location.PersonId == personId.Value);
		}

		return await query
			.OrderBy(location => location.Person.LastName)
			.ThenBy(location => location.Person.FirstName)
			.ThenBy(location => location.Nickname)
			.Select(location => new LocationListItem
			{
				Id = location.Id,
				PersonId = location.PersonId,
				PersonName = BuildFullName(location.Person.FirstName, location.Person.MiddleName, location.Person.LastName),
				Nickname = location.Nickname,
				PostalCode = location.PostalCode,
				Email = location.Email,
				PhoneNumber = location.PhoneNumber,
				AssignmentCount = location.Assignments.Count
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<LocationEditModel> GetLocationAsync(int? id, int? personId, CancellationToken cancellationToken = default)
	{
		if (!id.HasValue)
		{
			var personOptions = await GetPersonSelectOptionsAsync(cancellationToken, personId);
			var selectedPersonId = personId ?? personOptions.Select(option => TryParseInt(option.Value)).FirstOrDefault(value => value > 0);
			return await BuildLocationEditModelAsync(null, selectedPersonId, personOptions, cancellationToken);
		}

		var entity = await _dbContext.Locations
			.AsNoTracking()
			.Include(location => location.PostalAddress)
			.Include(location => location.MailingPostalAddress)
			.Include(location => location.Phones)
			.Include(location => location.Emails)
			.Include(location => location.Person)
				.ThenInclude(person => person.PostalAddress)
			.Include(location => location.Person)
				.ThenInclude(person => person.MailingPostalAddress)
			.Include(location => location.Person)
				.ThenInclude(person => person.Phones)
			.Include(location => location.Person)
				.ThenInclude(person => person.Emails)
			.FirstOrDefaultAsync(location => location.Id == id.Value, cancellationToken)
			?? throw new InvalidOperationException("The requested location was not found.");
		var personSelectOptions = await GetPersonSelectOptionsAsync(cancellationToken, entity.PersonId);

		return await BuildLocationEditModelAsync(entity, entity.PersonId, personSelectOptions, cancellationToken);
	}

	public async Task<int> SaveLocationAsync(LocationEditModel model, CancellationToken cancellationToken = default)
	{
		await EnsurePersonExistsAsync(model.PersonId, cancellationToken);

		var normalizedPhones = NormalizePhoneContacts(model.Phones);
		var normalizedEmails = NormalizeEmailContacts(model.Emails);
		var primaryPhone = normalizedPhones.FirstOrDefault();
		var primaryEmail = normalizedEmails.FirstOrDefault();
		var mailingAddressInput = GetAddressOrFallback(model.MailingAddress, model.PhysicalAddress);

		var physicalAddress = await ResolveOrCreatePostalAddressAsync(
			model.PhysicalAddress.CountryId,
			model.PhysicalAddress.StateProvinceId,
			model.PhysicalAddress.CountyId,
			model.PhysicalAddress.AddressLine1,
			model.PhysicalAddress.AddressLine2,
			model.PhysicalAddress.City,
			model.PhysicalAddress.PostalCode,
			cancellationToken);
		var mailingAddress = await ResolveOrCreatePostalAddressAsync(
			mailingAddressInput.CountryId,
			mailingAddressInput.StateProvinceId,
			mailingAddressInput.CountyId,
			mailingAddressInput.AddressLine1,
			mailingAddressInput.AddressLine2,
			mailingAddressInput.City,
			mailingAddressInput.PostalCode,
			cancellationToken);

		Location entity;
		if (model.Id.HasValue)
		{
			entity = await _dbContext.Locations
				.Include(location => location.Phones)
				.Include(location => location.Emails)
				.FirstOrDefaultAsync(location => location.Id == model.Id.Value, cancellationToken)
				?? throw new InvalidOperationException("The requested location was not found.");
			entity.Update(
				model.Nickname,
				physicalAddress.PostalAddress.Id,
				model.PhysicalAddress.AddressLine1,
				model.PhysicalAddress.AddressLine2,
				model.PhysicalAddress.City,
				physicalAddress.StateProvince.Code,
				model.PhysicalAddress.PostalCode,
				mailingAddress.PostalAddress.Id,
				mailingAddressInput.AddressLine1,
				mailingAddressInput.AddressLine2,
				mailingAddressInput.City,
				mailingAddress.StateProvince.Code,
				mailingAddressInput.PostalCode,
				primaryPhone?.PhoneNumber,
				primaryEmail?.EmailAddress,
				physicalAddress.Country.Name,
				mailingAddress.Country.Name);
		}
		else
		{
			entity = new Location(
				model.PersonId,
				model.Nickname,
				physicalAddress.PostalAddress.Id,
				model.PhysicalAddress.AddressLine1,
				model.PhysicalAddress.AddressLine2,
				model.PhysicalAddress.City,
				physicalAddress.StateProvince.Code,
				model.PhysicalAddress.PostalCode,
				mailingAddress.PostalAddress.Id,
				mailingAddressInput.AddressLine1,
				mailingAddressInput.AddressLine2,
				mailingAddressInput.City,
				mailingAddress.StateProvince.Code,
				mailingAddressInput.PostalCode,
				primaryPhone?.PhoneNumber,
				primaryEmail?.EmailAddress,
				physicalAddress.Country.Name,
				mailingAddress.Country.Name);
			_dbContext.Locations.Add(entity);
			await _dbContext.SaveChangesAsync(cancellationToken);
		}

		await SyncLocationPhonesAsync(entity, normalizedPhones, cancellationToken);
		await SyncLocationEmailsAsync(entity, normalizedEmails, cancellationToken);
		entity.UpdatePrimaryContactSnapshot(primaryPhone?.PhoneNumber, primaryEmail?.EmailAddress);
		await _dbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	public async Task DeleteLocationAsync(int id, CancellationToken cancellationToken = default)
	{
		var entity = await _dbContext.Locations
			.Include(location => location.Assignments)
			.FirstOrDefaultAsync(location => location.Id == id, cancellationToken)
			?? throw new InvalidOperationException("The requested location was not found.");

		if (entity.Assignments.Count > 0)
		{
			throw new InvalidOperationException("Locations with assignments cannot be removed.");
		}

		_dbContext.Locations.Remove(entity);
		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private async Task<LocationEditModel> BuildLocationEditModelAsync(
		Location? entity,
		int personId,
		IReadOnlyList<SelectOption> personOptions,
		CancellationToken cancellationToken)
	{
		var person = personId > 0
			? await _dbContext.People
				.AsNoTracking()
				.Include(item => item.PostalAddress)
				.Include(item => item.MailingPostalAddress)
				.Include(item => item.Phones)
				.Include(item => item.Emails)
				.FirstOrDefaultAsync(item => item.Id == personId, cancellationToken)
			: null;
		var emptyAddress = await BuildAddressInputModelAsync(null, null, null, null, null, null, cancellationToken);

		return new LocationEditModel
		{
			Id = entity?.Id,
			PersonId = personId,
			PersonName = person is null ? string.Empty : BuildFullName(person.FirstName, person.MiddleName, person.LastName),
			Nickname = entity?.Nickname ?? string.Empty,
			PhysicalAddress = entity is null
				? CloneAddressModel(person is null ? emptyAddress : await BuildAddressInputModelAsync(person.PostalAddress, person.State, person.AddressLine1, person.AddressLine2, person.City, person.PostalCode, cancellationToken))
				: await BuildAddressInputModelAsync(entity.PostalAddress, entity.State, entity.AddressLine1, entity.AddressLine2, entity.City, entity.PostalCode, cancellationToken),
			MailingAddress = entity is null
				? CloneAddressModel(person is null ? emptyAddress : await BuildAddressInputModelAsync(person.MailingPostalAddress, person.MailingState, person.MailingAddressLine1, person.MailingAddressLine2, person.MailingCity, person.MailingPostalCode, cancellationToken))
				: await BuildAddressInputModelAsync(entity.MailingPostalAddress, entity.MailingState, entity.MailingAddressLine1, entity.MailingAddressLine2, entity.MailingCity, entity.MailingPostalCode, cancellationToken),
			ProfilePhysicalAddress = person is null
				? CloneAddressModel(emptyAddress)
				: await BuildAddressInputModelAsync(person.PostalAddress, person.State, person.AddressLine1, person.AddressLine2, person.City, person.PostalCode, cancellationToken),
			ProfileMailingAddress = person is null
				? CloneAddressModel(emptyAddress)
				: await BuildAddressInputModelAsync(person.MailingPostalAddress, person.MailingState, person.MailingAddressLine1, person.MailingAddressLine2, person.MailingCity, person.MailingPostalCode, cancellationToken),
			Phones = entity?.Phones
				.OrderBy(phone => phone.SortOrder)
				.ThenBy(phone => phone.Id)
				.Select((phone, index) => new PhoneContactEditModel
			{
				Id = phone.Id,
				Label = ContactOptionCatalog.NormalizePhoneType(phone.Label) ?? ContactOptionCatalog.PhoneTypes.Home,
				PhoneNumber = phone.PhoneNumber,
				IsPrimary = index == 0,
					SortOrder = phone.SortOrder
				})
				.ToList()
				?? BuildDefaultPhoneModels(person?.Phones),
			Emails = entity?.Emails
				.OrderBy(email => email.SortOrder)
				.ThenBy(email => email.Id)
				.Select((email, index) => new EmailContactEditModel
			{
				Id = email.Id,
				Label = ContactOptionCatalog.NormalizeEmailType(email.Label) ?? ContactOptionCatalog.EmailTypes.Home,
				EmailAddress = email.EmailAddress,
				IsPrimary = index == 0,
					SortOrder = email.SortOrder
				})
				.ToList()
				?? BuildDefaultEmailModels(person?.Emails),
			ProfilePhones = person?.Phones
				.OrderBy(phone => phone.SortOrder)
				.ThenBy(phone => phone.Id)
				.Select((phone, index) => new PhoneContactEditModel
			{
				Id = phone.Id,
				Label = ContactOptionCatalog.NormalizePhoneType(phone.Label) ?? ContactOptionCatalog.PhoneTypes.Home,
				PhoneNumber = phone.PhoneNumber,
				IsPrimary = index == 0,
					SortOrder = phone.SortOrder
				})
				.ToList()
				?? [],
			ProfileEmails = person?.Emails
				.OrderBy(email => email.SortOrder)
				.ThenBy(email => email.Id)
				.Select((email, index) => new EmailContactEditModel
			{
				Id = email.Id,
				Label = ContactOptionCatalog.NormalizeEmailType(email.Label) ?? ContactOptionCatalog.EmailTypes.Home,
				EmailAddress = email.EmailAddress,
				IsPrimary = index == 0,
					SortOrder = email.SortOrder
				})
				.ToList()
				?? [],
			PersonOptions = personOptions
		};
	}

	private async Task<AddressInputModel> BuildAddressInputModelAsync(
		PostalAddress? address,
		string? legacyState,
		string? fallbackLine1,
		string? fallbackLine2,
		string? fallbackCity,
		string? fallbackPostalCode,
		CancellationToken cancellationToken)
	{
		var countryId = address?.CountryId ?? 0;
		if (countryId <= 0)
		{
			countryId = await GetDefaultCountryIdAsync(cancellationToken);
		}

		var stateProvinceId = address?.StateProvinceId
			?? (countryId > 0 ? await TryResolveLegacyStateProvinceIdAsync(legacyState, countryId, cancellationToken) : null)
			?? 0;
		var countryOptions = await GetCountrySelectOptionsAsync(cancellationToken);
		var stateProvinceOptions = countryId > 0
			? await GetStateProvinceSelectOptionsAsync(countryId, cancellationToken)
			: Array.Empty<SelectOption>();
		var countyOptions = stateProvinceId > 0
			? await GetCountySelectOptionsAsync(stateProvinceId, cancellationToken)
			: Array.Empty<SelectOption>();

		return new AddressInputModel
		{
			AddressLine1 = fallbackLine1 ?? address?.AddressLine1 ?? string.Empty,
			AddressLine2 = fallbackLine2 ?? address?.AddressLine2,
			City = fallbackCity ?? address?.City ?? string.Empty,
			CountryId = countryId,
			StateProvinceId = stateProvinceId,
			CountyId = address?.CountyId,
			PostalCode = fallbackPostalCode ?? address?.PostalCode ?? string.Empty,
			CountryOptions = countryOptions,
			StateProvinceOptions = stateProvinceOptions,
			CountyOptions = countyOptions
		};
	}

	public async Task<IReadOnlyList<SelectOption>> GetLocationSelectOptionsAsync(int? personId, int? includeLocationId = null, CancellationToken cancellationToken = default)
	{
		var query = _dbContext.Locations
			.AsNoTracking()
			.AsQueryable();

		if (personId.HasValue && personId.Value > 0)
		{
			query = query.Where(location => location.PersonId == personId.Value || (includeLocationId.HasValue && location.Id == includeLocationId.Value));
		}
		else if (includeLocationId.HasValue)
		{
			query = query.Where(location => location.Id == includeLocationId.Value);
		}
		else
		{
			return [];
		}

		return await query
			.OrderBy(location => location.Nickname)
			.Select(location => new SelectOption
			{
				Value = location.Id.ToString(),
				Label = $"{location.Nickname} ({location.PostalCode})"
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<SelectOption>> GetLocationPhoneSelectOptionsAsync(int? locationId, int? includePhoneId = null, CancellationToken cancellationToken = default)
	{
		var query = _dbContext.LocationPhones
			.AsNoTracking()
			.AsQueryable();

		if (locationId.HasValue && locationId.Value > 0)
		{
			query = query.Where(phone => phone.LocationId == locationId.Value || (includePhoneId.HasValue && phone.Id == includePhoneId.Value));
		}
		else if (includePhoneId.HasValue)
		{
			query = query.Where(phone => phone.Id == includePhoneId.Value);
		}
		else
		{
			return [];
		}

		return await query
			.OrderBy(phone => phone.SortOrder)
			.ThenBy(phone => phone.Label)
			.Select(phone => new SelectOption
			{
				Value = phone.Id.ToString(),
				Label = $"{ContactOptionCatalog.NormalizePhoneType(phone.Label) ?? ContactOptionCatalog.PhoneTypes.Home}: {phone.PhoneNumber}"
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<SelectOption>> GetLocationEmailSelectOptionsAsync(int? locationId, int? includeEmailId = null, CancellationToken cancellationToken = default)
	{
		var query = _dbContext.LocationEmails
			.AsNoTracking()
			.AsQueryable();

		if (locationId.HasValue && locationId.Value > 0)
		{
			query = query.Where(email => email.LocationId == locationId.Value || (includeEmailId.HasValue && email.Id == includeEmailId.Value));
		}
		else if (includeEmailId.HasValue)
		{
			query = query.Where(email => email.Id == includeEmailId.Value);
		}
		else
		{
			return [];
		}

		return await query
			.OrderBy(email => email.SortOrder)
			.ThenBy(email => email.Label)
			.Select(email => new SelectOption
			{
				Value = email.Id.ToString(),
				Label = $"{ContactOptionCatalog.NormalizeEmailType(email.Label) ?? ContactOptionCatalog.EmailTypes.Home}: {email.EmailAddress}"
			})
			.ToListAsync(cancellationToken);
	}

	private static AddressInputModel CloneAddressModel(AddressInputModel source)
	{
		return new AddressInputModel
		{
			AddressLine1 = source.AddressLine1,
			AddressLine2 = source.AddressLine2,
			City = source.City,
			CountryId = source.CountryId,
			StateProvinceId = source.StateProvinceId,
			CountyId = source.CountyId,
			PostalCode = source.PostalCode,
			CountryOptions = source.CountryOptions,
			StateProvinceOptions = source.StateProvinceOptions,
			CountyOptions = source.CountyOptions
		};
	}

	private static List<PhoneContactEditModel> BuildDefaultPhoneModels(IEnumerable<PersonPhone>? phones)
	{
		var items = phones?
			.OrderBy(phone => phone.SortOrder)
			.ThenBy(phone => phone.Id)
			.Select((phone, index) => new PhoneContactEditModel
			{
				Label = phone.Label,
				PhoneNumber = phone.PhoneNumber,
				IsPrimary = index == 0,
				SortOrder = phone.SortOrder
			})
			.ToList()
			?? [];

		if (items.Count == 0)
		{
			items.Add(new PhoneContactEditModel
			{
				Label = ContactOptionCatalog.PhoneTypes.Home,
				IsPrimary = true,
				SortOrder = 10
			});
		}

		return items;
	}

	private static List<EmailContactEditModel> BuildDefaultEmailModels(IEnumerable<PersonEmail>? emails)
	{
		var items = emails?
			.OrderBy(email => email.SortOrder)
			.ThenBy(email => email.Id)
			.Select((email, index) => new EmailContactEditModel
			{
				Label = email.Label,
				EmailAddress = email.EmailAddress,
				IsPrimary = index == 0,
				SortOrder = email.SortOrder
			})
			.ToList()
			?? [];

		if (items.Count == 0)
		{
			items.Add(new EmailContactEditModel
			{
				Label = ContactOptionCatalog.EmailTypes.Home,
				IsPrimary = true,
				SortOrder = 10
			});
		}

		return items;
	}

	private static List<PhoneContactEditModel> NormalizePhoneContacts(IEnumerable<PhoneContactEditModel> phones)
	{
		var normalized = phones
			.Where(phone => !string.IsNullOrWhiteSpace(phone.Label) || !string.IsNullOrWhiteSpace(phone.PhoneNumber))
			.Select((phone, index) => new PhoneContactEditModel
			{
				Id = phone.Id,
				Label = ContactOptionCatalog.NormalizePhoneType(phone.Label) ?? ContactOptionCatalog.PhoneTypes.Home,
				PhoneNumber = phone.PhoneNumber?.Trim() ?? string.Empty,
				IsPrimary = phone.IsPrimary,
				SortOrder = phone.SortOrder > 0 ? phone.SortOrder : (index + 1) * 10
			})
			.Where(phone => !string.IsNullOrWhiteSpace(phone.PhoneNumber))
			.ToList();

		var primaryIndex = normalized.FindIndex(phone => phone.IsPrimary);
		if (primaryIndex < 0 && normalized.Count > 0)
		{
			primaryIndex = 0;
		}

		for (var index = 0; index < normalized.Count; index++)
		{
			normalized[index].IsPrimary = index == primaryIndex;
		}

		normalized = normalized
			.OrderByDescending(phone => phone.IsPrimary)
			.ThenBy(phone => phone.SortOrder)
			.ThenBy(phone => phone.Label)
			.ToList();

		for (var index = 0; index < normalized.Count; index++)
		{
			normalized[index].SortOrder = (index + 1) * 10;
		}

		return normalized;
	}

	private static List<EmailContactEditModel> NormalizeEmailContacts(IEnumerable<EmailContactEditModel> emails)
	{
		var normalized = emails
			.Where(email => !string.IsNullOrWhiteSpace(email.Label) || !string.IsNullOrWhiteSpace(email.EmailAddress))
			.Select((email, index) => new EmailContactEditModel
			{
				Id = email.Id,
				Label = ContactOptionCatalog.NormalizeEmailType(email.Label) ?? ContactOptionCatalog.EmailTypes.Home,
				EmailAddress = email.EmailAddress?.Trim() ?? string.Empty,
				IsPrimary = email.IsPrimary,
				SortOrder = email.SortOrder > 0 ? email.SortOrder : (index + 1) * 10
			})
			.Where(email => !string.IsNullOrWhiteSpace(email.EmailAddress))
			.ToList();

		var primaryIndex = normalized.FindIndex(email => email.IsPrimary);
		if (primaryIndex < 0 && normalized.Count > 0)
		{
			primaryIndex = 0;
		}

		for (var index = 0; index < normalized.Count; index++)
		{
			normalized[index].IsPrimary = index == primaryIndex;
		}

		normalized = normalized
			.OrderByDescending(email => email.IsPrimary)
			.ThenBy(email => email.SortOrder)
			.ThenBy(email => email.Label)
			.ToList();

		for (var index = 0; index < normalized.Count; index++)
		{
			normalized[index].SortOrder = (index + 1) * 10;
		}

		return normalized;
	}

	private async Task SyncPersonPhonesAsync(Person person, IReadOnlyList<PhoneContactEditModel> phones, CancellationToken cancellationToken)
	{
		var existingPhones = person.Phones.ToDictionary(phone => phone.Id);
		var retainedIds = phones.Where(phone => phone.Id.HasValue).Select(phone => phone.Id!.Value).ToHashSet();

		foreach (var existing in person.Phones.Where(phone => !retainedIds.Contains(phone.Id)).ToList())
		{
			_dbContext.PersonPhones.Remove(existing);
		}

		foreach (var phone in phones)
		{
			if (phone.Id.HasValue && existingPhones.TryGetValue(phone.Id.Value, out var entity))
			{
				entity.Update(phone.Label, phone.PhoneNumber, phone.SortOrder);
				continue;
			}

			_dbContext.PersonPhones.Add(new PersonPhone(person.Id, phone.Label, phone.PhoneNumber, phone.SortOrder));
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private static AddressInputModel GetAddressOrFallback(AddressInputModel address, AddressInputModel fallback)
	{
		return IsAddressBlank(address) ? CloneAddressModel(fallback) : address;
	}

	private static bool IsAddressBlank(AddressInputModel address)
	{
		return address.StateProvinceId <= 0
			&& !address.CountyId.HasValue
			&& string.IsNullOrWhiteSpace(address.AddressLine1)
			&& string.IsNullOrWhiteSpace(address.AddressLine2)
			&& string.IsNullOrWhiteSpace(address.City)
			&& string.IsNullOrWhiteSpace(address.PostalCode);
	}

	private async Task SyncPersonEmailsAsync(Person person, IReadOnlyList<EmailContactEditModel> emails, CancellationToken cancellationToken)
	{
		var existingEmails = person.Emails.ToDictionary(email => email.Id);
		var retainedIds = emails.Where(email => email.Id.HasValue).Select(email => email.Id!.Value).ToHashSet();

		foreach (var existing in person.Emails.Where(email => !retainedIds.Contains(email.Id)).ToList())
		{
			_dbContext.PersonEmails.Remove(existing);
		}

		foreach (var email in emails)
		{
			if (email.Id.HasValue && existingEmails.TryGetValue(email.Id.Value, out var entity))
			{
				entity.Update(email.Label, email.EmailAddress, email.SortOrder);
				continue;
			}

			_dbContext.PersonEmails.Add(new PersonEmail(person.Id, email.Label, email.EmailAddress, email.SortOrder));
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private async Task SyncLocationPhonesAsync(Location location, IReadOnlyList<PhoneContactEditModel> phones, CancellationToken cancellationToken)
	{
		var existingPhones = location.Phones.ToDictionary(phone => phone.Id);
		var retainedIds = phones.Where(phone => phone.Id.HasValue).Select(phone => phone.Id!.Value).ToHashSet();

		foreach (var existing in location.Phones.Where(phone => !retainedIds.Contains(phone.Id)).ToList())
		{
			_dbContext.LocationPhones.Remove(existing);
		}

		foreach (var phone in phones)
		{
			if (phone.Id.HasValue && existingPhones.TryGetValue(phone.Id.Value, out var entity))
			{
				entity.Update(phone.Label, phone.PhoneNumber, phone.SortOrder);
				continue;
			}

			_dbContext.LocationPhones.Add(new LocationPhone(location.Id, phone.Label, phone.PhoneNumber, phone.SortOrder));
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private async Task SyncLocationEmailsAsync(Location location, IReadOnlyList<EmailContactEditModel> emails, CancellationToken cancellationToken)
	{
		var existingEmails = location.Emails.ToDictionary(email => email.Id);
		var retainedIds = emails.Where(email => email.Id.HasValue).Select(email => email.Id!.Value).ToHashSet();

		foreach (var existing in location.Emails.Where(email => !retainedIds.Contains(email.Id)).ToList())
		{
			_dbContext.LocationEmails.Remove(existing);
		}

		foreach (var email in emails)
		{
			if (email.Id.HasValue && existingEmails.TryGetValue(email.Id.Value, out var entity))
			{
				entity.Update(email.Label, email.EmailAddress, email.SortOrder);
				continue;
			}

			_dbContext.LocationEmails.Add(new LocationEmail(location.Id, email.Label, email.EmailAddress, email.SortOrder));
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private async Task EnsureLocationExistsAsync(int locationId, CancellationToken cancellationToken)
	{
		var exists = await _dbContext.Locations.AnyAsync(location => location.Id == locationId, cancellationToken);
		if (!exists)
		{
			throw new InvalidOperationException("The selected location was not found.");
		}
	}

	private async Task EnsureLocationPhoneBelongsToLocationAsync(int locationId, int? locationPhoneId, CancellationToken cancellationToken)
	{
		if (!locationPhoneId.HasValue)
		{
			return;
		}

		var exists = await _dbContext.LocationPhones.AnyAsync(phone => phone.Id == locationPhoneId && phone.LocationId == locationId, cancellationToken);
		if (!exists)
		{
			throw new InvalidOperationException("The selected location phone was not found.");
		}
	}

	private async Task EnsureLocationEmailBelongsToLocationAsync(int locationId, int? locationEmailId, CancellationToken cancellationToken)
	{
		if (!locationEmailId.HasValue)
		{
			return;
		}

		var exists = await _dbContext.LocationEmails.AnyAsync(email => email.Id == locationEmailId && email.LocationId == locationId, cancellationToken);
		if (!exists)
		{
			throw new InvalidOperationException("The selected location email was not found.");
		}
	}
}
