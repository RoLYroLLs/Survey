using Survey.Domain;

namespace Survey.Domain.Tests;

public class DomainBehaviorTests
{
	[Fact]
	public void SurveyVersion_Rejects_Version_Numbers_Below_One()
	{
		var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new SurveyVersion(1, "Version 0", 0, false));

		Assert.Equal("versionNumber", exception.ParamName);
	}

	[Fact]
	public void SurveyAssignment_IsExpired_Is_True_When_Expiry_Equals_Current_Time()
	{
		var now = DateTimeOffset.UtcNow;
		var assignment = new SurveyAssignment(1, 2, 3, 4, "token-123", now, null);

		Assert.True(assignment.IsExpired(now));
		Assert.False(assignment.IsExpired(now.AddSeconds(-1)));
	}

	[Fact]
	public void SurveySection_Normalizes_Negative_Sort_Order_To_Zero()
	{
		var section = new SurveySection(3, "Needs", null, -5);

		Assert.Equal(0, section.SortOrder);
	}

	[Fact]
	public void SurveyQuestion_Normalizes_Negative_Sort_Order_To_Zero()
	{
		var question = new SurveyQuestion(4, "What support do you need?", null, SurveyQuestionType.LongText, true, -2);

		Assert.Equal(0, question.SortOrder);
	}

	[Fact]
	public void SurveyDefinition_SetArchived_Updates_State()
	{
		var definition = new SurveyDefinition("Community Intake", "Archive test");

		definition.SetArchived(true);

		Assert.True(definition.IsArchived);
	}

	[Fact]
	public void SurveyVersion_SetArchived_Updates_State()
	{
		var version = new SurveyVersion(1, "Community Intake v1", 1, true);

		version.SetArchived(true);

		Assert.True(version.IsArchived);
	}

	[Fact]
	public void PostalCodeNormalizer_Extracts_Five_Digit_Zip()
	{
		var normalized = PostalCodeNormalizer.Normalize("33131-1234", "zip");
		var extracted = PostalCodeNormalizer.Extract("Miami, FL 33131-1234");

		Assert.Equal("33131", normalized);
		Assert.Equal("33131", extracted);
	}

	[Fact]
	public void Goal_Rejects_End_Date_Before_Start_Date()
	{
		var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
			new Goal("South Florida Goal", null, 1, null, 500, new DateOnly(2026, 1, 31), new DateOnly(2026, 1, 1)));

		Assert.Equal("endDate", exception.ParamName);
	}

	[Fact]
	public void TenantInvitation_Is_Not_Usable_After_Accept()
	{
		var invitation = new TenantInvitation(1, "invitee@example.com", TenantRole.User, "token-hash", DateTimeOffset.UtcNow.AddDays(1), "admin-user");

		invitation.Accept();

		Assert.False(invitation.IsUsable(DateTimeOffset.UtcNow));
		Assert.NotNull(invitation.AcceptedUtc);
	}

	[Fact]
	public void TenantInvitation_Is_Not_Usable_After_Revoke()
	{
		var invitation = new TenantInvitation(1, "invitee@example.com", TenantRole.Admin, "token-hash", DateTimeOffset.UtcNow.AddDays(1), "admin-user");

		invitation.Revoke();

		Assert.False(invitation.IsUsable(DateTimeOffset.UtcNow));
		Assert.NotNull(invitation.RevokedUtc);
	}

	[Fact]
	public void PermissionDefaults_Give_Admin_User_Management_Capabilities()
	{
		var permissions = PermissionDefaults.GetTenantPermissions(TenantRole.Admin);

		Assert.Contains(TenantPermissionKeys.UsersView, permissions);
		Assert.Contains(TenantPermissionKeys.UsersInvite, permissions);
		Assert.Contains(TenantPermissionKeys.UsersChangeRole, permissions);
		Assert.Contains(TenantPermissionKeys.UsersManagePermissions, permissions);
	}
}
