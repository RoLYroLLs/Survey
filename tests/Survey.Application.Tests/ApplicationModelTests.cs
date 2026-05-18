using System.ComponentModel.DataAnnotations;
using Survey.Application.Models;

namespace Survey.Application.Tests;

public class ApplicationModelTests
{
	[Fact]
	public void PlatformUserEditModel_IsNew_Is_True_Only_When_Id_Is_Missing()
	{
		Assert.True(new PlatformUserEditModel().IsNew);
		Assert.True(new PlatformUserEditModel { Id = " " }.IsNew);
		Assert.False(new PlatformUserEditModel { Id = "user-1" }.IsNew);
	}

	[Fact]
	public void SurveyVersionEditModel_Requires_Survey_And_Version_Number()
	{
		var model = new SurveyVersionEditModel
		{
			SurveyDefinitionId = 0,
			DisplayName = "Community Intake v1",
			VersionNumber = 0
		};

		var validationResults = new List<ValidationResult>();
		var isValid = Validator.TryValidateObject(model, new ValidationContext(model), validationResults, validateAllProperties: true);

		Assert.False(isValid);
		Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(SurveyVersionEditModel.SurveyDefinitionId)));
		Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(SurveyVersionEditModel.VersionNumber)));
	}

	[Fact]
	public void TenantUserPermissionOverrideEditModel_Computes_Effective_Grant_From_Default_Mode()
	{
		var model = new TenantUserPermissionOverrideEditModel
		{
			DefaultGranted = true,
			OverrideMode = TenantPermissionOverrideModes.Default
		};

		Assert.True(model.EffectiveGranted);

		model.OverrideMode = TenantPermissionOverrideModes.Deny;
		Assert.False(model.EffectiveGranted);

		model.OverrideMode = TenantPermissionOverrideModes.Allow;
		Assert.True(model.EffectiveGranted);
	}
}
