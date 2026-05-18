using System.Security.Cryptography;

namespace Survey.Infrastructure.Identity;

public static class UserAvatarPalette
{
	private static readonly string[] Colors =
	[
		"#0d8f81",
		"#1f6feb",
		"#7c3aed",
		"#c2410c",
		"#be185d",
		"#15803d",
		"#0369a1",
		"#7f1d1d",
		"#4f46e5",
		"#b45309",
		"#0f766e",
		"#4338ca"
	];

	public static void EnsureAssigned(ApplicationUser user)
	{
		if (!string.IsNullOrWhiteSpace(user.AvatarColorHex))
		{
			return;
		}

		user.AvatarColorHex = Colors[RandomNumberGenerator.GetInt32(Colors.Length)];
	}
}
