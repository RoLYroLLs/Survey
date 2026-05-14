using System.ComponentModel.DataAnnotations;
using Survey.Application.Models;

namespace Survey.Application.Tests;

public class ApplicationModelTests
{
	[Fact]
	public void EmployeeEditModel_IsNew_Is_True_Only_When_Id_Is_Missing()
	{
		Assert.True(new EmployeeEditModel().IsNew);
		Assert.True(new EmployeeEditModel { Id = " " }.IsNew);
		Assert.False(new EmployeeEditModel { Id = "employee-1" }.IsNew);
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
}
