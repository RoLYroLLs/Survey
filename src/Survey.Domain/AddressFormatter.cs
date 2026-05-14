namespace Survey.Domain;

public static class AddressFormatter
{
	public static string Format(
		string addressLine1,
		string? addressLine2,
		string city,
		string? state,
		string postalCode,
		string? countryName = null)
	{
		var street = string.Join(", ", new[] { addressLine1.Trim(), CleanOptional(addressLine2) }
			.Where(static part => !string.IsNullOrWhiteSpace(part)));
		var stateValue = CleanOptional(state);
		var cityState = string.IsNullOrWhiteSpace(stateValue)
			? city.Trim()
			: $"{city.Trim()}, {stateValue}";
		var cityStatePostal = string.Join(" ", new[] { cityState, postalCode.Trim() }
			.Where(static part => !string.IsNullOrWhiteSpace(part)));
		var country = CleanOptional(countryName);
		var includeCountry = !string.IsNullOrWhiteSpace(country)
			&& !string.Equals(country, "United States of America", StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(country, "United States", StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(country, "USA", StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(country, "US", StringComparison.OrdinalIgnoreCase);

		return string.Join(", ", new[] { street, cityStatePostal, includeCountry ? country : null }
			.Where(static part => !string.IsNullOrWhiteSpace(part)));
	}

	private static string? CleanOptional(string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}
}
