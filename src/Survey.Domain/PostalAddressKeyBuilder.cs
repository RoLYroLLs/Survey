namespace Survey.Domain;

public static class PostalAddressKeyBuilder
{
	public static string Build(
		string countryIso2Code,
		string? stateProvinceCode,
		string addressLine1,
		string? addressLine2,
		string city,
		string postalCode)
	{
		return string.Join("|", new[]
		{
			NormalizePart(countryIso2Code),
			NormalizePart(stateProvinceCode),
			NormalizePart(addressLine1),
			NormalizePart(addressLine2),
			NormalizePart(city),
			NormalizePart(postalCode)
		});
	}

	private static string NormalizePart(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		return string.Join(" ", value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
			.ToUpperInvariant();
	}
}
