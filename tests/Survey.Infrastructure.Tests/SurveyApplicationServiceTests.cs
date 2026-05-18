using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Survey.Application.Models;
using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure.Identity;
using Survey.Infrastructure.Persistence;

namespace Survey.Infrastructure.Tests;

public class SurveyApplicationServiceTests
{
	[Fact]
	public void NormalizeProvider_Defaults_To_Sqlite_For_Missing_Or_Unknown_Values()
	{
		Assert.Equal("Sqlite", ServiceCollectionExtensions.NormalizeProvider(null));
		Assert.Equal("Sqlite", ServiceCollectionExtensions.NormalizeProvider(string.Empty));
		Assert.Equal("Sqlite", ServiceCollectionExtensions.NormalizeProvider("postgres"));
		Assert.Equal("SqlServer", ServiceCollectionExtensions.NormalizeProvider("SqlServer"));
		Assert.Equal("SqlServer", ServiceCollectionExtensions.NormalizeProvider("sqlserver"));
	}

	[Fact]
	public async Task GetSiteAppearanceAsync_Defaults_To_Coastal_Current_When_No_Row_Exists()
	{
		await using var harness = await TestHarness.CreateAsync();

		var appearance = await harness.ExperienceService.GetSiteAppearanceAsync();

		Assert.Equal(SiteThemePresetCatalog.DefaultPresetKey, appearance.ThemePresetKey);
		Assert.Equal("Coastal Current", appearance.ThemePresetName);
		Assert.Contains("--app-primary", appearance.CssVariablesBlock, StringComparison.Ordinal);
	}

	[Fact]
	public async Task SaveSiteSettingsAsync_Persists_Selected_Theme()
	{
		await using var harness = await TestHarness.CreateAsync();

		await harness.AdministrationService.SaveSiteSettingsAsync(new SiteSettingsEditModel
		{
			ThemePresetKey = "harbor-blue"
		});

		var appearance = await harness.ExperienceService.GetSiteAppearanceAsync();
		var entity = await harness.DbContext.SiteSettings.SingleAsync();

		Assert.Equal("harbor-blue", entity.ThemePresetKey);
		Assert.Equal("harbor-blue", appearance.ThemePresetKey);
		Assert.Equal("Harbor Blue", appearance.ThemePresetName);
	}

	[Fact]
	public async Task SaveSurveyVersionAsync_Throws_When_Version_Has_Been_Assigned()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			harness.AdministrationService.SaveSurveyVersionAsync(new SurveyVersionEditModel
			{
				Id = seed.VersionId,
				SurveyDefinitionId = seed.DefinitionId,
				DisplayName = "Updated Version",
				VersionNumber = 2,
				IsPublished = true
			}));

		Assert.Contains("locked", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task GetPublicSessionAsync_Returns_Sections_And_Questions_In_Sort_Order()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		var session = await harness.ExperienceService.GetPublicSessionAsync(seed.Token);

		Assert.NotNull(session);
		Assert.Equal(["Contact Preferences", "Household Details"], session!.Sections.Select(section => section.Title));
		Assert.Equal(["Preferred language", "Can we text you?"], session.Sections[0].Questions.Select(question => question.Prompt));
		Assert.Equal(["Which services do you need?", "Anything else we should know?"], session.Sections[1].Questions.Select(question => question.Prompt));
	}

	[Fact]
	public async Task SubmitAsync_Throws_When_Required_Multi_Select_Is_Missing()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		var submission = new SurveySubmissionModel
		{
			AssignmentId = seed.AssignmentId,
			Token = seed.Token,
			Contact = BuildContact(seed, "Alicia", "M.", "Lopez", "101 New Street", "555-0123", "Afternoons", "alicia@example.com"),
			Answers =
			[
				new SurveyAnswerInputModel
				{
					QuestionId = seed.LanguageQuestionId,
					SelectedOptionId = seed.OptionEnglishId
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.TextQuestionId,
					TextAnswer = "Happy to participate."
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.YesNoQuestionId,
					YesNoAnswer = true
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.MultiQuestionId
				}
			]
		};

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.ExperienceService.SubmitAsync(submission, null));

		Assert.Contains("At least one selection is required", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task SubmitAsync_Persists_Employee_Audit_And_Contact_Snapshot()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		var submission = new SurveySubmissionModel
		{
			AssignmentId = seed.AssignmentId,
			Token = seed.Token,
			IsStaffMode = true,
			Contact = BuildContact(seed, "Alicia", "M.", "Lopez", "101 New Street", "555-0999", "Evenings", "alicia.updated@example.com"),
			Answers =
			[
				new SurveyAnswerInputModel
				{
					QuestionId = seed.YesNoQuestionId,
					YesNoAnswer = false
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.MultiQuestionId,
					SelectedOptionIds = [seed.OptionFoodId, seed.OptionHousingId]
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.TextQuestionId,
					TextAnswer = "Please call after work."
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.LanguageQuestionId,
					SelectedOptionId = seed.OptionEnglishId
				}
			]
		};

		var result = await harness.ExperienceService.SubmitAsync(submission, "employee-42");

		Assert.True(result.Succeeded);
		Assert.NotNull(result.ResponseId);

		var response = await harness.DbContext.SurveyResponses
			.Include(entity => entity.Answers)
			.SingleAsync();

		Assert.True(response.SubmittedByEmployee);
		Assert.Equal("employee-42", response.SubmittedByUserId);
		Assert.Equal("Alicia", response.RespondentFirstName);
		Assert.Equal("M.", response.RespondentMiddleName);
		Assert.Equal("Lopez", response.RespondentLastName);
		Assert.Equal("101 New Street, Miami, FL 33101", response.RespondentHomeAddress);
		Assert.Equal("33101", response.RespondentPostalCode);
		Assert.NotNull(response.RespondentPostalAddressId);
		Assert.Null(response.RespondentCountyFipsSnapshot);
		Assert.Equal("555-0999", response.RespondentPhoneNumber);
		Assert.Equal(ContactOptionCatalog.BestTimes.Evening, response.RespondentBestTimeToContact);
		Assert.Equal(ContactOptionCatalog.PreferredContactMethods.Call, response.RespondentPreferredContactMethod);
		Assert.Equal("alicia.updated@example.com", response.RespondentEmail);
		Assert.Equal("Community Intake", response.SurveyNameSnapshot);
		Assert.Equal("Community Intake v1", response.SurveyVersionNameSnapshot);
		Assert.Equal(4, response.Answers.Count);
	}

	[Fact]
	public async Task SaveEmployeeAsync_Throws_For_Unknown_Role()
	{
		await using var harness = await TestHarness.CreateAsync();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			harness.AdministrationService.SaveEmployeeAsync(new EmployeeEditModel
			{
				FirstName = "Pat",
				LastName = "Jordan",
				Email = "pat@example.com",
				Password = "TempPass123",
				ConfirmPassword = "TempPass123",
				RoleName = "Supervisor"
			}));

		Assert.Contains("Admin or Employee role", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task GetSurveySectionAsync_New_ForLockedVersion_IsBlockedImmediately()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		var model = await harness.AdministrationService.GetSurveySectionAsync(null, seed.VersionId);

		Assert.True(model.IsLocked);
		Assert.Equal(seed.VersionId, model.SurveyVersionId);
		Assert.Equal(seed.DefinitionId, model.SurveyDefinitionId);
	}

	[Fact]
	public async Task GetQuestionOptionAsync_Flags_Unsupported_Question_Types()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		var yesNoModel = await harness.AdministrationService.GetQuestionOptionAsync(null, seed.YesNoQuestionId);
		var longTextModel = await harness.AdministrationService.GetQuestionOptionAsync(null, seed.TextQuestionId);

		Assert.False(yesNoModel.SupportsOptions);
		Assert.Equal(SurveyQuestionType.YesNo, yesNoModel.QuestionType);
		Assert.False(longTextModel.SupportsOptions);
		Assert.Equal(SurveyQuestionType.LongText, longTextModel.QuestionType);
	}

	[Fact]
	public async Task SaveSurveyDefinitionAsync_Archives_And_Filters_Surveys()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();
		var model = await harness.AdministrationService.GetSurveyDefinitionAsync(seed.DefinitionId);
		model.IsArchived = true;

		await harness.AdministrationService.SaveSurveyDefinitionAsync(model);

		var active = await harness.AdministrationService.GetSurveyDefinitionsAsync();
		var archived = await harness.AdministrationService.GetSurveyDefinitionsAsync(true);

		Assert.Empty(active);
		Assert.Single(archived);
		Assert.True(archived[0].IsArchived);
	}

	[Fact]
	public async Task SaveSurveyVersionAsync_Allows_Archiving_A_Locked_Version()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();
		var model = await harness.AdministrationService.GetSurveyVersionAsync(seed.VersionId, null);
		model.IsArchived = true;

		await harness.AdministrationService.SaveSurveyVersionAsync(model);

		var active = await harness.AdministrationService.GetSurveyVersionsAsync(seed.DefinitionId);
		var archived = await harness.AdministrationService.GetSurveyVersionsAsync(seed.DefinitionId, true);

		Assert.Empty(active);
		Assert.Single(archived);
		Assert.True(archived[0].IsArchived);
	}

	[Fact]
	public async Task SaveSurveyVersionAsync_Archives_And_Filters_Versions()
	{
		await using var harness = await TestHarness.CreateAsync();

		var definitionId = await harness.AdministrationService.SaveSurveyDefinitionAsync(new SurveyDefinitionEditModel
		{
			Name = "Benefits Follow Up",
			Description = "Unassigned version archive test."
		});

		var versionId = await harness.AdministrationService.SaveSurveyVersionAsync(new SurveyVersionEditModel
		{
			SurveyDefinitionId = definitionId,
			DisplayName = "Benefits Follow Up v1",
			VersionNumber = 1,
			IsPublished = false
		});

		var model = await harness.AdministrationService.GetSurveyVersionAsync(versionId, null);
		model.IsArchived = true;

		await harness.AdministrationService.SaveSurveyVersionAsync(model);

		var active = await harness.AdministrationService.GetSurveyVersionsAsync(definitionId);
		var archived = await harness.AdministrationService.GetSurveyVersionsAsync(definitionId, true);

		Assert.Empty(active);
		Assert.Single(archived);
		Assert.True(archived[0].IsArchived);
	}

	[Fact]
	public async Task GetStateProvincesAsync_Returns_CountyCounts_When_Filtered_By_Country()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		var states = await harness.AdministrationService.GetStateProvincesAsync(seed.CountryId);

		var florida = Assert.Single(states);
		Assert.Equal("United States of America", florida.CountryFilterName);
		Assert.Equal(2, florida.CountyCount);
	}

	[Fact]
	public async Task GetAssignmentsAsync_Returns_Newest_First_On_Sqlite()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		var laterAssignment = new SurveyAssignment(seed.LocationId, seed.LocationPhoneId, seed.LocationEmailId, seed.VersionId, $"token-{Guid.NewGuid():N}", DateTimeOffset.UtcNow.AddDays(4), "admin-user");
		harness.DbContext.SurveyAssignments.Add(laterAssignment);
		await harness.DbContext.SaveChangesAsync();

		var earlierAssignment = await harness.DbContext.SurveyAssignments.SingleAsync(assignment => assignment.Id == seed.AssignmentId);
		harness.DbContext.Entry(earlierAssignment).Property(assignment => assignment.CreatedUtc).CurrentValue = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
		harness.DbContext.Entry(laterAssignment).Property(assignment => assignment.CreatedUtc).CurrentValue = new DateTimeOffset(2026, 5, 2, 12, 0, 0, TimeSpan.Zero);
		await harness.DbContext.SaveChangesAsync();

		var assignments = await harness.AdministrationService.GetAssignmentsAsync();

		Assert.Equal([laterAssignment.Id, earlierAssignment.Id], assignments.Select(assignment => assignment.Id).Take(2).ToArray());
	}

	[Fact]
	public async Task ImportZipCountyMappingsAsync_And_Goal_Report_Work_Together()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		await harness.AdministrationService.ImportZipCountyMappingsAsync(new ZipCountyImportModel
		{
			CsvContent = """
				ZIP,COUNTY,COUNTYNAME,STATE,RES_RATIO
				33101,12086,Miami-Dade County,FL,1
				32801,12095,Orange County,FL,1
				"""
		});

		var areaId = await harness.AdministrationService.SaveAreaAsync(new AreaEditModel
		{
			Name = "South Florida",
			Description = "Miami-Dade coverage.",
			SelectedCountyFips = ["12086"]
		});

		await harness.AdministrationService.SaveGoalAsync(new GoalEditModel
		{
			Name = "500 South Florida Responses",
			AreaId = areaId,
			TargetResponseCount = 500,
			StartDate = new DateOnly(2026, 1, 1),
			EndDate = new DateOnly(2026, 12, 31)
		});

		var submission = new SurveySubmissionModel
		{
			AssignmentId = seed.AssignmentId,
			Token = seed.Token,
			Contact = BuildContact(seed, "Taylor", null, "Rivers", "123 Main Street", "555-0100", "Morning", "taylor@example.com", "33101"),
			Answers =
			[
				new SurveyAnswerInputModel
				{
					QuestionId = seed.YesNoQuestionId,
					YesNoAnswer = true
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.MultiQuestionId,
					SelectedOptionIds = [seed.OptionFoodId]
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.TextQuestionId,
					TextAnswer = "Ready"
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.LanguageQuestionId,
					SelectedOptionId = seed.OptionEnglishId
				}
			]
		};

		var submitResult = await harness.ExperienceService.SubmitAsync(submission, null);
		Assert.True(submitResult.Succeeded);

		var response = await harness.DbContext.SurveyResponses.SingleAsync();
		Assert.Equal("33101", response.RespondentPostalCode);
		Assert.Equal("12086", response.RespondentCountyFipsSnapshot);
		Assert.Equal("Miami-Dade County", response.RespondentCountyNameSnapshot);

		var report = await harness.AdministrationService.GetReportingOverviewAsync();

		Assert.Equal(1, report.TotalResponses);
		Assert.Equal(1, report.MappedResponses);
		Assert.Equal(0, report.UnmappedResponses);
		Assert.Single(report.AreaResponses);
		Assert.Equal(1, report.AreaResponses[0].ResponseCount);
		Assert.Single(report.GoalProgress);
		Assert.Equal(1, report.GoalProgress[0].CompletedResponses);
	}

	[Fact]
	public async Task Geography_Filters_Return_State_County_Address_And_Area_Slices()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		await harness.AdministrationService.SaveAreaAsync(new AreaEditModel
		{
			Name = "South Florida",
			Description = "Miami-Dade coverage.",
			SelectedCountyFips = ["12086"]
		});

		await harness.AdministrationService.SaveAreaAsync(new AreaEditModel
		{
			Name = "Central Florida",
			Description = "Orange coverage.",
			SelectedCountyFips = ["12095"]
		});

		var states = await harness.AdministrationService.GetStateProvincesAsync(seed.CountryId);
		var state = Assert.Single(states);
		Assert.Equal(2, state.CountyCount);

		var counties = await harness.AdministrationService.GetCountiesAsync(seed.StateProvinceId);
		Assert.Equal(2, counties.Count);
		Assert.All(counties, county => Assert.Equal("Florida (FL)", county.StateProvinceFilterName));

		var addresses = await harness.AdministrationService.GetPostalAddressesAsync(countyId: seed.MiamiDadeCountyId);
		var address = Assert.Single(addresses);
		Assert.Equal(seed.MiamiDadeCountyId, address.CountyId);
		Assert.Equal("Miami-Dade County", address.CountyName);

		var areas = await harness.AdministrationService.GetAreasAsync(seed.OrangeCountyId);
		var area = Assert.Single(areas);
		Assert.Equal("Central Florida", area.Name);
		Assert.Equal("Orange County", area.CountyNameFilter);
	}

	[Fact]
	public async Task SaveGoalAsync_Allows_All_Areas_And_Counts_Responses_Across_Counties()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		await harness.AdministrationService.ImportZipCountyMappingsAsync(new ZipCountyImportModel
		{
			CsvContent = """
				ZIP,COUNTY,COUNTYNAME,STATE,RES_RATIO
				33101,12086,Miami-Dade County,FL,1
				32801,12095,Orange County,FL,1
				"""
		});

		await harness.AdministrationService.SaveAreaAsync(new AreaEditModel
		{
			Name = "South Florida",
			Description = "Miami-Dade coverage.",
			SelectedCountyFips = ["12086"]
		});

		await harness.AdministrationService.SaveAreaAsync(new AreaEditModel
		{
			Name = "Central Florida",
			Description = "Orange coverage.",
			SelectedCountyFips = ["12095"]
		});

		await harness.AdministrationService.SaveGoalAsync(new GoalEditModel
		{
			Name = "500 Florida Responses",
			AreaId = null,
			TargetResponseCount = 500,
			StartDate = new DateOnly(2026, 1, 1),
			EndDate = new DateOnly(2026, 12, 31)
		});

		var firstSubmission = new SurveySubmissionModel
		{
			AssignmentId = seed.AssignmentId,
			Token = seed.Token,
			Contact = BuildContact(seed, "Taylor", null, "Rivers", "123 Main Street", "555-0100", "Morning", "taylor@example.com", "33101"),
			Answers =
			[
				new SurveyAnswerInputModel
				{
					QuestionId = seed.YesNoQuestionId,
					YesNoAnswer = true
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.MultiQuestionId,
					SelectedOptionIds = [seed.OptionFoodId]
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.TextQuestionId,
					TextAnswer = "Ready"
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.LanguageQuestionId,
					SelectedOptionId = seed.OptionEnglishId
				}
			]
		};

		var firstResult = await harness.ExperienceService.SubmitAsync(firstSubmission, null);
		Assert.True(firstResult.Succeeded);

		var secondAssignment = new SurveyAssignment(seed.LocationId, seed.LocationPhoneId, seed.LocationEmailId, seed.VersionId, $"token-{Guid.NewGuid():N}", DateTimeOffset.UtcNow.AddDays(2), "admin-user");
		harness.DbContext.SurveyAssignments.Add(secondAssignment);
		await harness.DbContext.SaveChangesAsync();

		var secondSubmission = new SurveySubmissionModel
		{
			AssignmentId = secondAssignment.Id,
			Token = secondAssignment.PublicToken,
			Contact = BuildContact(seed, "Taylor", null, "Rivers", "500 Oak Avenue", "555-0101", "Afternoon", "taylor@example.com", "32801", city: "Orlando"),
			Answers =
			[
				new SurveyAnswerInputModel
				{
					QuestionId = seed.YesNoQuestionId,
					YesNoAnswer = false
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.MultiQuestionId,
					SelectedOptionIds = [seed.OptionHousingId]
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.TextQuestionId,
					TextAnswer = "Need support."
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.LanguageQuestionId,
					SelectedOptionId = seed.OptionEnglishId
				}
			]
		};

		var secondResult = await harness.ExperienceService.SubmitAsync(secondSubmission, null);
		Assert.True(secondResult.Succeeded);

		var goals = await harness.AdministrationService.GetGoalsAsync();
		var goal = Assert.Single(goals);
		Assert.Null(goal.AreaName);

		var report = await harness.AdministrationService.GetReportingOverviewAsync();

		Assert.Equal(2, report.TotalResponses);
		Assert.Equal(2, report.MappedResponses);
		Assert.Equal(0, report.UnmappedResponses);
		Assert.Equal(2, report.AreaResponses.Count);
		Assert.Single(report.GoalProgress);
		Assert.Null(report.GoalProgress[0].AreaName);
		Assert.Equal(2, report.GoalProgress[0].CompletedResponses);
	}

	[Fact]
	public async Task GetResponsesAsync_Returns_Newest_First_On_Sqlite()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		var earlierResponse = new SurveyResponse(
			seed.AssignmentId,
			null,
			false,
			"Taylor",
			null,
			"Rivers",
			null,
			"123 Main Street",
			null,
			"Miami",
			"FL",
			"33101",
			null,
			"123 Main Street",
			null,
			"Miami",
			"FL",
			"33101",
			null,
			null,
			null,
			"555-0100",
			ContactOptionCatalog.PhoneTypes.Home,
			ContactOptionCatalog.BestTimes.Morning,
			ContactOptionCatalog.PreferredContactMethods.Call,
			"taylor@example.com",
			ContactOptionCatalog.EmailTypes.Home,
			"Community Intake",
			"Community Intake v1",
			"United States of America",
			"United States of America");
		harness.DbContext.SurveyResponses.Add(earlierResponse);

		var laterAssignment = new SurveyAssignment(seed.LocationId, seed.LocationPhoneId, seed.LocationEmailId, seed.VersionId, $"token-{Guid.NewGuid():N}", DateTimeOffset.UtcNow.AddDays(5), "admin-user");
		harness.DbContext.SurveyAssignments.Add(laterAssignment);
		await harness.DbContext.SaveChangesAsync();

		var laterResponse = new SurveyResponse(
			laterAssignment.Id,
			"employee-42",
			true,
			"Jordan",
			null,
			"Lee",
			null,
			"500 Oak Avenue",
			null,
			"Orlando",
			"FL",
			"32801",
			null,
			"500 Oak Avenue",
			null,
			"Orlando",
			"FL",
			"32801",
			null,
			null,
			null,
			"555-0101",
			ContactOptionCatalog.PhoneTypes.Home,
			ContactOptionCatalog.BestTimes.Afternoon,
			ContactOptionCatalog.PreferredContactMethods.Call,
			"jordan@example.com",
			ContactOptionCatalog.EmailTypes.Home,
			"Community Intake",
			"Community Intake v1",
			"United States of America",
			"United States of America");
		harness.DbContext.SurveyResponses.Add(laterResponse);
		await harness.DbContext.SaveChangesAsync();

		harness.DbContext.Entry(earlierResponse).Property(response => response.SubmittedUtc).CurrentValue = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
		harness.DbContext.Entry(laterResponse).Property(response => response.SubmittedUtc).CurrentValue = new DateTimeOffset(2026, 5, 2, 12, 0, 0, TimeSpan.Zero);
		await harness.DbContext.SaveChangesAsync();

		var responses = await harness.AdministrationService.GetResponsesAsync();

		Assert.Equal([laterResponse.Id, earlierResponse.Id], responses.Select(response => response.Id).Take(2).ToArray());
	}

	[Fact]
	public async Task SubmitAsync_Uses_Selected_County_When_Provided()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		var submission = new SurveySubmissionModel
		{
			AssignmentId = seed.AssignmentId,
			Token = seed.Token,
			Contact = BuildContact(
				seed,
				"Alicia",
				"M.",
				"Lopez",
				"101 New Street",
				"555-0123",
				"Afternoons",
				"alicia@example.com",
				countyId: seed.MiamiDadeCountyId),
			Answers =
			[
				new SurveyAnswerInputModel
				{
					QuestionId = seed.LanguageQuestionId,
					SelectedOptionId = seed.OptionEnglishId
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.TextQuestionId,
					TextAnswer = "Happy to participate."
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.YesNoQuestionId,
					YesNoAnswer = true
				},
				new SurveyAnswerInputModel
				{
					QuestionId = seed.MultiQuestionId,
					SelectedOptionIds = [seed.OptionFoodId]
				}
			]
		};

		var result = await harness.ExperienceService.SubmitAsync(submission, null);

		Assert.True(result.Succeeded);

		var response = await harness.DbContext.SurveyResponses
			.Include(entity => entity.RespondentPostalAddress)
			.SingleAsync();

		Assert.Equal("12086", response.RespondentCountyFipsSnapshot);
		Assert.Equal("Miami-Dade County", response.RespondentCountyNameSnapshot);
		Assert.Equal(seed.MiamiDadeCountyId, response.RespondentPostalAddress!.CountyId);
	}

	private static RespondentContactModel BuildContact(
		SeedData seed,
		string firstName,
		string? middleName,
		string lastName,
		string addressLine1,
		string phoneNumber,
		string? bestTimeToContact,
		string email,
		string? postalCode = "33101",
		string? addressLine2 = null,
		string city = "Miami",
		string state = "FL",
		int? countyId = null)
	{
		return new RespondentContactModel
		{
			FirstName = firstName,
			MiddleName = middleName,
			LastName = lastName,
			PhoneNumber = phoneNumber,
			PhoneLabel = ContactOptionCatalog.PhoneTypes.Home,
			BestTimeToContact = bestTimeToContact,
			PreferredContactMethod = ContactOptionCatalog.PreferredContactMethods.Call,
			Email = email,
			EmailLabel = ContactOptionCatalog.EmailTypes.Home,
			PhysicalAddress = BuildAddress(seed, addressLine1, addressLine2, city, postalCode, countyId),
			MailingAddress = BuildAddress(seed, addressLine1, addressLine2, city, postalCode, countyId),
			ProfilePhysicalAddress = BuildAddress(seed, "123 Main Street", null, "Miami", "33101", seed.MiamiDadeCountyId),
			ProfileMailingAddress = BuildAddress(seed, "123 Main Street", null, "Miami", "33101", seed.MiamiDadeCountyId)
		};
	}

	private static AddressInputModel BuildAddress(
		SeedData seed,
		string addressLine1,
		string? addressLine2,
		string city,
		string? postalCode,
		int? countyId)
	{
		return new AddressInputModel
		{
			AddressLine1 = addressLine1,
			AddressLine2 = addressLine2,
			City = city,
			CountryId = seed.CountryId,
			StateProvinceId = seed.StateProvinceId,
			CountyId = countyId,
			PostalCode = postalCode
		};
	}

	private sealed class TestHarness : IAsyncDisposable
	{
		private readonly ServiceProvider _provider;
		private readonly IServiceScope _scope;
		private readonly string _databasePath;

		private TestHarness(ServiceProvider provider, IServiceScope scope, string databasePath)
		{
			_provider = provider;
			_scope = scope;
			_databasePath = databasePath;
			DbContext = _scope.ServiceProvider.GetRequiredService<SurveyDbContext>();
			AdministrationService = _scope.ServiceProvider.GetRequiredService<ISurveyAdministrationService>();
			ExperienceService = _scope.ServiceProvider.GetRequiredService<ISurveyExperienceService>();
		}

		public SurveyDbContext DbContext { get; }
		public ISurveyAdministrationService AdministrationService { get; }
		public ISurveyExperienceService ExperienceService { get; }

		public static async Task<TestHarness> CreateAsync()
		{
			var databasePath = Path.Combine(Path.GetTempPath(), $"survey-tests-{Guid.NewGuid():N}.db");
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["Database:Provider"] = "Sqlite",
					["ConnectionStrings:Default"] = $"Data Source={databasePath}"
				})
				.Build();

			var services = new ServiceCollection();
			services.AddDataProtection();
			services.AddLogging();
			services.AddSurveyInfrastructure(configuration);

			var provider = services.BuildServiceProvider();
			var scope = provider.CreateScope();
			var harness = new TestHarness(provider, scope, databasePath);

			await harness.DbContext.Database.EnsureDeletedAsync();
			await harness.DbContext.Database.EnsureCreatedAsync();

			return harness;
		}

		public async Task<SeedData> SeedSurveyAsync()
		{
			var country = new Country("United States of America", "US", "USA");
			DbContext.Countries.Add(country);
			await DbContext.SaveChangesAsync();

			var stateProvince = new StateProvince(country.Id, "Florida", "FL", "State");
			DbContext.StateProvinces.Add(stateProvince);
			await DbContext.SaveChangesAsync();

			var miamiDadeCounty = new County(stateProvince.Id, "Miami-Dade County", "12086");
			var orangeCounty = new County(stateProvince.Id, "Orange County", "12095");
			DbContext.Counties.AddRange(miamiDadeCounty, orangeCounty);
			await DbContext.SaveChangesAsync();

			var postalAddress = new PostalAddress(country.Id, stateProvince.Id, miamiDadeCounty.Id, "123 Main Street", null, "Miami", "33101", country.Iso2Code, stateProvince.Code, country.Name);
			DbContext.PostalAddresses.Add(postalAddress);
			await DbContext.SaveChangesAsync();

			var person = new Person(
				"Taylor",
				null,
				"Rivers",
				postalAddress.Id,
				"123 Main Street",
				null,
				"Miami",
				"FL",
				"33101",
				postalAddress.Id,
				"123 Main Street",
				null,
				"Miami",
				"FL",
				"33101",
				"555-0100",
				"Morning",
				"Call",
				"taylor@example.com",
				country.Name,
				country.Name);
			DbContext.People.Add(person);
			await DbContext.SaveChangesAsync();

			var personPhone = new PersonPhone(person.Id, ContactOptionCatalog.PhoneTypes.Home, "555-0100", 10);
			var personEmail = new PersonEmail(person.Id, ContactOptionCatalog.EmailTypes.Home, "taylor@example.com", 10);
			DbContext.PersonPhones.Add(personPhone);
			DbContext.PersonEmails.Add(personEmail);
			await DbContext.SaveChangesAsync();

			var location = new Location(
				person.Id,
				"Imported Location",
				postalAddress.Id,
				"123 Main Street",
				null,
				"Miami",
				"FL",
				"33101",
				postalAddress.Id,
				"123 Main Street",
				null,
				"Miami",
				"FL",
				"33101",
				"555-0100",
				"taylor@example.com",
				country.Name,
				country.Name);
			DbContext.Locations.Add(location);
			await DbContext.SaveChangesAsync();

			var locationPhone = new LocationPhone(location.Id, "Home", "555-0100", 10);
			var locationEmail = new LocationEmail(location.Id, "Primary", "taylor@example.com", 10);
			DbContext.LocationPhones.Add(locationPhone);
			DbContext.LocationEmails.Add(locationEmail);
			await DbContext.SaveChangesAsync();

			var definition = new SurveyDefinition("Community Intake", "Household intake and follow-up details.");
			DbContext.SurveyDefinitions.Add(definition);
			await DbContext.SaveChangesAsync();

			var version = new SurveyVersion(definition.Id, "Community Intake v1", 1, true);
			DbContext.SurveyVersions.Add(version);
			await DbContext.SaveChangesAsync();

			var detailsSection = new SurveySection(version.Id, "Household Details", "Core household questions.", 20);
			var preferencesSection = new SurveySection(version.Id, "Contact Preferences", "How should we reach you?", 10);
			DbContext.SurveySections.AddRange(detailsSection, preferencesSection);
			await DbContext.SaveChangesAsync();

			var multiQuestion = new SurveyQuestion(detailsSection.Id, "Which services do you need?", null, SurveyQuestionType.MultiSelect, true, 30);
			var textQuestion = new SurveyQuestion(detailsSection.Id, "Anything else we should know?", null, SurveyQuestionType.LongText, false, 40);
			var languageQuestion = new SurveyQuestion(preferencesSection.Id, "Preferred language", null, SurveyQuestionType.SingleChoice, true, 10);
			var yesNoQuestion = new SurveyQuestion(preferencesSection.Id, "Can we text you?", null, SurveyQuestionType.YesNo, true, 20);
			DbContext.SurveyQuestions.AddRange(multiQuestion, textQuestion, languageQuestion, yesNoQuestion);
			await DbContext.SaveChangesAsync();

			var optionFood = new QuestionOption(multiQuestion.Id, "Food assistance", 10);
			var optionHousing = new QuestionOption(multiQuestion.Id, "Housing support", 20);
			var optionEnglish = new QuestionOption(languageQuestion.Id, "English", 10);
			var optionSpanish = new QuestionOption(languageQuestion.Id, "Spanish", 20);
			DbContext.QuestionOptions.AddRange(optionFood, optionHousing, optionEnglish, optionSpanish);
			await DbContext.SaveChangesAsync();

			var token = $"token-{Guid.NewGuid():N}";
			var assignment = new SurveyAssignment(location.Id, locationPhone.Id, locationEmail.Id, version.Id, token, DateTimeOffset.UtcNow.AddDays(2), "admin-user");
			DbContext.SurveyAssignments.Add(assignment);
			await DbContext.SaveChangesAsync();

			return new SeedData(
				country.Id,
				stateProvince.Id,
				miamiDadeCounty.Id,
				orangeCounty.Id,
				definition.Id,
				version.Id,
				person.Id,
				location.Id,
				locationPhone.Id,
				locationEmail.Id,
				assignment.Id,
				token,
				yesNoQuestion.Id,
				multiQuestion.Id,
				textQuestion.Id,
				languageQuestion.Id,
				optionFood.Id,
				optionHousing.Id,
				optionEnglish.Id,
				optionSpanish.Id);
		}

		public async ValueTask DisposeAsync()
		{
			_scope.Dispose();
			await _provider.DisposeAsync();

			if (File.Exists(_databasePath))
			{
				try
				{
					File.Delete(_databasePath);
				}
				catch (IOException)
				{
				}
				catch (UnauthorizedAccessException)
				{
				}
			}
		}
	}

	private sealed record SeedData(
		int CountryId,
		int StateProvinceId,
		int MiamiDadeCountyId,
		int OrangeCountyId,
		int DefinitionId,
		int VersionId,
		int PersonId,
		int LocationId,
		int LocationPhoneId,
		int LocationEmailId,
		int AssignmentId,
		string Token,
		int YesNoQuestionId,
		int MultiQuestionId,
		int TextQuestionId,
		int LanguageQuestionId,
		int OptionFoodId,
		int OptionHousingId,
		int OptionEnglishId,
		int OptionSpanishId);
}
