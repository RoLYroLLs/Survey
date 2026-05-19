namespace Survey.Infrastructure.Identity;

public class ApplicationUserPasskey
{
	public byte[] CredentialId { get; set; } = [];

	public string UserId { get; set; } = string.Empty;

	public string Data { get; set; } = string.Empty;
}
