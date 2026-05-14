using System.Text.RegularExpressions;

namespace Survey.Domain;

public static partial class PostalCodeNormalizer
{
	private static readonly Regex UnitedStatesPostalCodeRegex = UnitedStatesPostalCodePattern();
	private static readonly Regex GenericPostalCodeRegex = GenericPostalCodePattern();

	public static string? Normalize(string? value, string paramName)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var trimmed = value.Trim();
		var unitedStatesMatch = UnitedStatesPostalCodeRegex.Match(trimmed);
		if (unitedStatesMatch.Success)
		{
			return unitedStatesMatch.Groups["zip"].Value;
		}

		if (!GenericPostalCodeRegex.IsMatch(trimmed))
		{
			throw new ArgumentException("The postal code is invalid.", paramName);
		}

		return trimmed.ToUpperInvariant();
	}

	public static string? Extract(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var match = UnitedStatesPostalCodeRegex.Match(value);
		return match.Success ? match.Groups["zip"].Value : null;
	}

	[GeneratedRegex(@"(?<!\d)(?<zip>\d{5})(?:-\d{4})?(?!\d)", RegexOptions.Compiled)]
	private static partial Regex UnitedStatesPostalCodePattern();

	[GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9 \-]{1,19}$", RegexOptions.Compiled)]
	private static partial Regex GenericPostalCodePattern();
}
