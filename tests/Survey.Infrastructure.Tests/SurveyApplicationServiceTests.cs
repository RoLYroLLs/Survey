using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Survey.Application.Models;
using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure.Identity;
using Survey.Infrastructure.Persistence;
using Survey.Infrastructure.Security;
using Survey.Infrastructure.Services;

namespace Survey.Infrastructure.Tests;

public class SurveyApplicationServiceTests
{
	private static PagedQuery CreatePagedRequest(int offset = 0, int limit = PagedQuery.MaxLimit)
	{
		return new PagedQuery
		{
			Offset = offset,
			Limit = limit
		};
	}

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
		await harness.CompleteInitialSetupAsync(SiteThemePresetCatalog.DefaultPresetKey, "harbor-blue");

		await harness.AdministrationService.SaveSiteSettingsAsync(new SiteSettingsEditModel
		{
			TenantName = "Test Tenant",
			ThemePresetKey = "harbor-blue"
		});

		var appearance = await harness.ExperienceService.GetSiteAppearanceAsync();
		var entity = await harness.DbContext.TenantSettings.SingleAsync();

		Assert.Equal("harbor-blue", entity.ThemePresetKey);
		Assert.Equal("harbor-blue", appearance.ThemePresetKey);
		Assert.Equal("Harbor Blue", appearance.ThemePresetName);
	}

	[Fact]
	public async Task SaveSiteSettingsAsync_Throws_When_Current_User_Is_Not_Tenant_Admin_Or_Owner()
	{
		await using var harness = await TestHarness.CreateAsync();
		await harness.CompleteInitialSetupAsync(SiteThemePresetCatalog.DefaultPresetKey, "harbor-blue");

		var standardUser = new ApplicationUser
		{
			UserName = "tenant.user@example.com",
			Email = "tenant.user@example.com",
			EmailConfirmed = true,
			FirstName = "Tenant",
			LastName = "User",
			IsPlatformSuperAdmin = false,
			IsPlatformUserEnabled = false
		};

		var createResult = await harness.UserManager.CreateAsync(standardUser, "TempPass123!");
		Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(static error => error.Description)));

		var membership = new TenantMembership(harness.PrimaryTenantId, standardUser.Id, TenantRole.User);
		harness.DbContext.TenantMemberships.Add(membership);
		await harness.DbContext.SaveChangesAsync();
		await harness.SetCurrentUserAsync(standardUser, membership.Id);

		var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
			harness.AdministrationService.SaveSiteSettingsAsync(new SiteSettingsEditModel
			{
				TenantName = "Test Tenant",
				ThemePresetKey = "harbor-blue"
			}));

		Assert.Contains("tenant owner", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task GetTenantThemeOptionsAsync_Returns_Empty_Until_InitialSetup_Is_Completed()
	{
		await using var harness = await TestHarness.CreateAsync();

		var beforeSetup = await harness.AdministrationService.GetTenantThemeOptionsAsync();
		Assert.Empty(beforeSetup);

		await harness.CompleteInitialSetupAsync(SiteThemePresetCatalog.DefaultPresetKey, "harbor-blue");

		var afterSetup = await harness.AdministrationService.GetTenantThemeOptionsAsync();
		Assert.Equal(2, afterSetup.Count);
		Assert.Contains(afterSetup, option => option.Key == SiteThemePresetCatalog.DefaultPresetKey);
		Assert.Contains(afterSetup, option => option.Key == "harbor-blue");
	}

	[Fact]
	public async Task SaveTenantGeographyVisibilityAsync_Throws_When_Current_User_Is_Not_Tenant_Admin_Or_Owner()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		var standardUser = new ApplicationUser
		{
			UserName = "tenant.user@example.com",
			Email = "tenant.user@example.com",
			EmailConfirmed = true,
			FirstName = "Tenant",
			LastName = "User",
			IsPlatformSuperAdmin = false,
			IsPlatformUserEnabled = false
		};

		var createResult = await harness.UserManager.CreateAsync(standardUser, "TempPass123!");
		Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(static error => error.Description)));

		var membership = new TenantMembership(harness.PrimaryTenantId, standardUser.Id, TenantRole.User);
		harness.DbContext.TenantMemberships.Add(membership);
		await harness.DbContext.SaveChangesAsync();
		await harness.SetCurrentUserAsync(standardUser, membership.Id);

		var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
			harness.TenantAdministrationService.SaveTenantGeographyVisibilityAsync(new TenantGeographyVisibilityEditModel
			{
				VisibleCountryIds = [seed.CountryId]
			}));

		Assert.Contains("tenant owner", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task GetTenantCountySelectOptionsAsync_Respects_TenantVisibility()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		await harness.TenantAdministrationService.SaveTenantGeographyVisibilityAsync(new TenantGeographyVisibilityEditModel
		{
			VisibleCountyIds = [seed.MiamiDadeCountyId]
		});

		var options = await harness.TenantAdministrationService.GetTenantCountySelectOptionsAsync(seed.StateProvinceId);

		Assert.Single(options);
		Assert.Contains("Miami-Dade County", options[0].Label, StringComparison.Ordinal);
	}

	[Fact]
	public async Task SaveAreaAsync_Rejects_County_Outside_TenantVisibility()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		await harness.TenantAdministrationService.SaveTenantGeographyVisibilityAsync(new TenantGeographyVisibilityEditModel
		{
			VisibleCountyIds = [seed.MiamiDadeCountyId]
		});

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			harness.TenantAdministrationService.SaveAreaAsync(new AreaEditModel
			{
				Name = "Orange Area",
				Description = "Should fail",
				SelectedCountyFips = ["12095"]
			}));

		Assert.Contains("was not found in the counties list", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task GeographyDataSeeder_Seeds_All_UnitedStatesCounties_And_DistrictOfColumbia()
	{
		await using var harness = await TestHarness.CreateAsync();

		await harness.GeographySeeder.SeedAsync(forceRun: true);

		var countyCount = await harness.DbContext.Counties.CountAsync();
		var districtOfColumbia = await harness.DbContext.StateProvinces
			.Include(stateProvince => stateProvince.Counties)
			.FirstOrDefaultAsync(stateProvince => stateProvince.Code == "DC");

		Assert.Equal(3144, countyCount);
		Assert.NotNull(districtOfColumbia);
		Assert.Equal("District", districtOfColumbia!.SubdivisionType);
		Assert.Single(districtOfColumbia.Counties);
		Assert.Equal("District of Columbia", districtOfColumbia.Counties.Single().Name);
	}

	[Fact]
	public async Task InitialSetupSeeder_Seeds_All_Tracked_Stages()
	{
		await using var harness = await TestHarness.CreateAsync();
		var selectedThemeKeys = new[] { SiteThemePresetCatalog.DefaultPresetKey, "harbor-blue" };
		const string defaultThemeKey = "harbor-blue";

		var updates = new Dictionary<string, InitialSeedingProgressUpdate>(StringComparer.Ordinal);
		await harness.InitialSetupSeeder.SeedAsync(selectedThemeKeys, defaultThemeKey, update =>
		{
			updates[update.StageKey] = update;
			return Task.CompletedTask;
		});

		Assert.True(await harness.InitialSetupSeeder.IsSeededAsync());
		Assert.Equal(
			InitialSeedingStages.Ordered.Select(stage => stage.Key).OrderBy(static key => key),
			updates.Keys.OrderBy(static key => key));
		Assert.All(updates.Values, update => Assert.True(update.IsComplete));
		Assert.Equal(selectedThemeKeys.Length, await harness.DbContext.PlatformThemes.CountAsync());
		var siteSetting = await harness.DbContext.SiteSettings.SingleOrDefaultAsync(setting => setting.Id == SiteSetting.DefaultId);
		Assert.NotNull(siteSetting);
		Assert.Equal(defaultThemeKey, siteSetting!.ThemePresetKey);
		Assert.NotEmpty(await harness.DbContext.Countries.ToListAsync());
		Assert.NotEmpty(await harness.DbContext.StateProvinces.ToListAsync());
		Assert.NotEmpty(await harness.DbContext.Counties.ToListAsync());
		Assert.NotEmpty(await harness.DbContext.ZipCountyLookups.ToListAsync());
	}

	[Fact]
	public async Task DisablePlatformThemeAsync_Replaces_Default_And_Tenant_Theme_When_Theme_Is_In_Use()
	{
		await using var harness = await TestHarness.CreateAsync();
		await harness.CompleteInitialSetupAsync(SiteThemePresetCatalog.DefaultPresetKey, "harbor-blue");
		harness.DbContext.TenantSettings.Add(new TenantSetting(harness.PrimaryTenantId, SiteThemePresetCatalog.DefaultPresetKey));
		await harness.DbContext.SaveChangesAsync();

		var currentTheme = await harness.DbContext.PlatformThemes.SingleAsync(theme => theme.Key == SiteThemePresetCatalog.DefaultPresetKey);
		var replacementTheme = await harness.DbContext.PlatformThemes.SingleAsync(theme => theme.Key == "harbor-blue");

		await harness.PlatformAdministrationService.SetPlatformThemeEnabledAsync(currentTheme.Id, false, replacementTheme.Id);

		var siteSetting = await harness.DbContext.SiteSettings.SingleAsync(setting => setting.Id == SiteSetting.DefaultId);
		var tenantSetting = await harness.DbContext.TenantSettings.SingleAsync();
		var disabledTheme = await harness.DbContext.PlatformThemes.SingleAsync(theme => theme.Id == currentTheme.Id);

		Assert.Equal("harbor-blue", siteSetting.ThemePresetKey);
		Assert.Equal("harbor-blue", tenantSetting.ThemePresetKey);
		Assert.False(disabledTheme.IsEnabled);
	}

	[Fact]
	public async Task DeletePlatformThemeAsync_Replaces_Default_And_Tenant_Theme_When_Theme_Is_In_Use()
	{
		await using var harness = await TestHarness.CreateAsync();
		await harness.CompleteInitialSetupAsync(SiteThemePresetCatalog.DefaultPresetKey, "harbor-blue");
		harness.DbContext.TenantSettings.Add(new TenantSetting(harness.PrimaryTenantId, SiteThemePresetCatalog.DefaultPresetKey));
		await harness.DbContext.SaveChangesAsync();

		var currentTheme = await harness.DbContext.PlatformThemes.SingleAsync(theme => theme.Key == SiteThemePresetCatalog.DefaultPresetKey);
		var replacementTheme = await harness.DbContext.PlatformThemes.SingleAsync(theme => theme.Key == "harbor-blue");

		await harness.PlatformAdministrationService.DeletePlatformThemeAsync(currentTheme.Id, replacementTheme.Id);

		var siteSetting = await harness.DbContext.SiteSettings.SingleAsync(setting => setting.Id == SiteSetting.DefaultId);
		var tenantSetting = await harness.DbContext.TenantSettings.SingleAsync();

		Assert.Equal("harbor-blue", siteSetting.ThemePresetKey);
		Assert.Equal("harbor-blue", tenantSetting.ThemePresetKey);
		Assert.False(await harness.DbContext.PlatformThemes.AnyAsync(theme => theme.Id == currentTheme.Id));
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
		var currentUserId = await harness.DbContext.Users.Select(user => user.Id).SingleAsync();

		var response = await harness.DbContext.SurveyResponses
			.Include(entity => entity.Answers)
			.SingleAsync();

		Assert.True(response.SubmittedByEmployee);
		Assert.Equal(currentUserId, response.SubmittedByUserId);
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
	public async Task SavePlatformUserAsync_Throws_When_Final_SuperAdmin_Would_Be_Removed()
	{
		await using var harness = await TestHarness.CreateAsync();

		var operatorUser = new ApplicationUser
		{
			UserName = "operator@example.com",
			Email = "operator@example.com",
			EmailConfirmed = true,
			FirstName = "Platform",
			LastName = "Operator",
			IsPlatformSuperAdmin = false,
			IsPlatformUserEnabled = true
		};

		var createResult = await harness.UserManager.CreateAsync(operatorUser, "TempPass123!");
		Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(static error => error.Description)));

		harness.DbContext.PlatformUserPermissions.AddRange(
			new PlatformUserPermission(operatorUser.Id, PlatformPermissionKeys.UsersView),
			new PlatformUserPermission(operatorUser.Id, PlatformPermissionKeys.UsersManage),
			new PlatformUserPermission(operatorUser.Id, PlatformPermissionKeys.PermissionsManage));
		await harness.DbContext.SaveChangesAsync();
		await harness.SetCurrentUserAsync(operatorUser);

		var model = await harness.PlatformAdministrationService.GetPlatformUserAsync(harness.PrimaryUserId);
		model.IsPlatformSuperAdmin = false;

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.PlatformAdministrationService.SavePlatformUserAsync(model));

		Assert.Contains("final enabled platform super admin", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task SavePlatformUserAsync_Throws_When_Bootstrap_Platform_Owner_Would_Be_Disabled_Or_Demoted()
	{
		await using var harness = await TestHarness.CreateAsync();

		var bootstrapOwner = await harness.UserManager.Users.FirstAsync(user => user.Id == harness.PrimaryUserId);
		bootstrapOwner.IsBootstrapPlatformOwner = true;
		var updateResult = await harness.UserManager.UpdateAsync(bootstrapOwner);
		Assert.True(updateResult.Succeeded, string.Join("; ", updateResult.Errors.Select(static error => error.Description)));

		var operatorUser = new ApplicationUser
		{
			UserName = "operator@example.com",
			Email = "operator@example.com",
			EmailConfirmed = true,
			FirstName = "Platform",
			LastName = "Operator",
			IsPlatformSuperAdmin = true,
			IsPlatformUserEnabled = true
		};

		var createResult = await harness.UserManager.CreateAsync(operatorUser, "TempPass123!");
		Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(static error => error.Description)));

		await harness.SetCurrentUserAsync(operatorUser);

		var model = await harness.PlatformAdministrationService.GetPlatformUserAsync(harness.PrimaryUserId);
		model.IsPlatformUserEnabled = false;

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.PlatformAdministrationService.SavePlatformUserAsync(model));

		Assert.Contains("bootstrap platform owner", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task GetPeopleAsync_Isolated_To_The_Active_Tenant()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		var otherTenant = new Tenant("Other Tenant");
		harness.DbContext.Tenants.Add(otherTenant);
		await harness.DbContext.SaveChangesAsync();

		harness.TenantExecutionContext.UseTenant(otherTenant.Id);
		var otherAddress = new PostalAddress(seed.CountryId, seed.StateProvinceId, seed.OrangeCountyId, "500 Oak Avenue", null, "Orlando", "32801", "US", "FL", "United States of America");
		harness.DbContext.PostalAddresses.Add(otherAddress);
		await harness.DbContext.SaveChangesAsync();

		var otherPerson = new Person(
			"Jordan",
			null,
			"Lee",
			otherAddress.Id,
			"500 Oak Avenue",
			null,
			"Orlando",
			"FL",
			"32801",
			otherAddress.Id,
			"500 Oak Avenue",
			null,
			"Orlando",
			"FL",
			"32801",
			"555-0111",
			"Afternoon",
			"Call",
			"jordan@example.com",
			"United States of America",
			"United States of America");
		harness.DbContext.People.Add(otherPerson);
		await harness.DbContext.SaveChangesAsync();

		var people = (await harness.TenantAdministrationService.GetPeopleAsync(CreatePagedRequest())).Items;

		Assert.Contains(people, item => item.Id == seed.PersonId);
		Assert.DoesNotContain(people, item => item.Id == otherPerson.Id);
	}

	[Fact]
	public async Task SearchTenantAsync_Returns_Only_Current_Tenant_Results()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		var otherTenant = new Tenant("Other Tenant");
		harness.DbContext.Tenants.Add(otherTenant);
		await harness.DbContext.SaveChangesAsync();

		harness.TenantExecutionContext.UseTenant(otherTenant.Id);
		var otherAddress = new PostalAddress(seed.CountryId, seed.StateProvinceId, seed.MiamiDadeCountyId, "999 Other Street", null, "Miami", "33101", "US", "FL", "United States of America");
		harness.DbContext.PostalAddresses.Add(otherAddress);
		await harness.DbContext.SaveChangesAsync();

		var otherPerson = new Person(
			"Taylor",
			null,
			"Outside",
			otherAddress.Id,
			"999 Other Street",
			null,
			"Miami",
			"FL",
			"33101",
			otherAddress.Id,
			"999 Other Street",
			null,
			"Miami",
			"FL",
			"33101",
			"555-0199",
			"Morning",
			"Call",
			"outside@example.com",
			"United States of America",
			"United States of America");
		harness.DbContext.People.Add(otherPerson);
		await harness.DbContext.SaveChangesAsync();

		harness.TenantExecutionContext.UseTenant(harness.PrimaryTenantId);
		var results = await harness.TenantAdministrationService.SearchTenantAsync("Taylor");

		var peopleSection = Assert.Single(results.Sections, section => section.Key == "people");
		Assert.Contains(peopleSection.Items, item => item.Url == $"/app/people/{seed.PersonId}");
		Assert.DoesNotContain(peopleSection.Items, item => item.Title.Contains("Outside", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task GetPeopleAsync_Defaults_To_Ten_And_Loads_The_Next_Batch_Without_Overlap()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();
		var addressId = await harness.DbContext.PostalAddresses.Select(address => address.Id).SingleAsync();

		for (var index = 0; index < 14; index++)
		{
			harness.DbContext.People.Add(new Person(
				$"Person{index:00}",
				null,
				"Batch",
				addressId,
				"123 Main Street",
				null,
				"Miami",
				"FL",
				"33101",
				addressId,
				"123 Main Street",
				null,
				"Miami",
				"FL",
				"33101",
				$"555-01{index:00}",
				"Morning",
				"Call",
				$"person{index:00}@example.com",
				"United States of America",
				"United States of America"));
		}

		await harness.DbContext.SaveChangesAsync();

		var firstPage = await harness.TenantAdministrationService.GetPeopleAsync(new PagedQuery());
		var secondPage = await harness.TenantAdministrationService.GetPeopleAsync(new PagedQuery
		{
			Offset = firstPage.Items.Count,
			Limit = PagedQuery.DefaultLimit
		});

		Assert.Equal(PagedQuery.DefaultLimit, firstPage.Items.Count);
		Assert.Equal(15, firstPage.TotalCount);
		Assert.True(firstPage.HasMore);
		Assert.Equal(5, secondPage.Items.Count);
		Assert.Equal(15, secondPage.TotalCount);
		Assert.False(secondPage.HasMore);
		var combinedIds = firstPage.Items.Select(item => item.Id).Concat(secondPage.Items.Select(item => item.Id)).ToArray();
		Assert.Empty(firstPage.Items.Select(item => item.Id).Intersect(secondPage.Items.Select(item => item.Id)));
		Assert.Contains(seed.PersonId, combinedIds);
	}

	[Fact]
	public async Task GetCountriesAsync_Search_Filter_Still_Uses_Paged_Total_Counts()
	{
		await using var harness = await TestHarness.CreateAsync();

		for (var index = 0; index < 12; index++)
		{
			var code = ((char)('A' + index)).ToString();
			harness.DbContext.Countries.Add(new Country($"Alpha Country {index:00}", $"Q{code}", $"Q{code}{code}"));
		}

		harness.DbContext.Countries.Add(new Country("Beta Country", "BC", "BET"));
		await harness.DbContext.SaveChangesAsync();

		var firstPage = await harness.PlatformAdministrationService.GetCountriesAsync(new PagedQuery(), "Alpha");
		var secondPage = await harness.PlatformAdministrationService.GetCountriesAsync(new PagedQuery
		{
			Offset = firstPage.Items.Count,
			Limit = PagedQuery.DefaultLimit
		}, "Alpha");

		Assert.Equal(PagedQuery.DefaultLimit, firstPage.Items.Count);
		Assert.Equal(12, firstPage.TotalCount);
		Assert.True(firstPage.HasMore);
		Assert.Equal(2, secondPage.Items.Count);
		Assert.Equal(12, secondPage.TotalCount);
		Assert.False(secondPage.HasMore);
		Assert.All(firstPage.Items, item => Assert.Contains("Alpha", item.Name, StringComparison.OrdinalIgnoreCase));
		Assert.All(secondPage.Items, item => Assert.Contains("Alpha", item.Name, StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task CreateTenantInvitationAsync_Reissues_Pending_Invitations_And_Audits_The_Change()
	{
		await using var harness = await TestHarness.CreateAsync();
		const string baseUrl = "https://survey.example.test";

		var firstInvite = await harness.TenantAdministrationService.CreateTenantInvitationAsync(new TenantUserInviteModel
		{
			Email = "invitee@example.com",
			Role = TenantRole.User
		}, baseUrl);

		var secondInvite = await harness.TenantAdministrationService.CreateTenantInvitationAsync(new TenantUserInviteModel
		{
			Email = "invitee@example.com",
			Role = TenantRole.User
		}, baseUrl);

		Assert.NotEqual(firstInvite.Token, secondInvite.Token);

		var invitations = (await harness.TenantAdministrationService.GetTenantInvitationsAsync(CreatePagedRequest())).Items;
		Assert.Equal(2, invitations.Count);
		Assert.Single(invitations, invitation => invitation.IsPending);
		Assert.Single(invitations, invitation => invitation.RevokedUtc is not null);

		var auditLogs = (await harness.PlatformAdministrationService.GetAuditLogsAsync(CreatePagedRequest(), plane: "tenant")).Items;
		Assert.Contains(auditLogs, log => log.ActionType == "tenant.user.invited");
		Assert.Contains(auditLogs, log => log.ActionType == "tenant.invitation.revoked");
	}

	[Fact]
	public async Task CreateTenantInvitationAsync_Queues_Tracked_Email_And_Links_It_To_A_Background_Operation()
	{
		await using var harness = await TestHarness.CreateAsync();
		const string baseUrl = "https://survey.example.test";

		await harness.TenantAdministrationService.CreateTenantInvitationAsync(new TenantUserInviteModel
		{
			Email = "invitee@example.com",
			Role = TenantRole.User
		}, baseUrl);

		var invitation = await harness.DbContext.TenantInvitations.SingleAsync(item => item.Email == "invitee@example.com");
		var email = await harness.DbContext.OutboundEmails.SingleAsync(item =>
			item.SourceType == OutboundEmailSourceTypes.TenantInvitation
			&& item.SourceId == invitation.Id.ToString());
		var operation = await harness.BackgroundOperationsService.GetBackgroundOperationAsync(email.BackgroundOperationId!.Value);

		Assert.Equal("invitee@example.com", email.RecipientEmail);
		Assert.Equal(harness.PrimaryTenantId, email.TenantId);
		Assert.False(string.IsNullOrWhiteSpace(email.TrackingToken));
		Assert.Contains("/email/track/open/", email.HtmlBody, StringComparison.Ordinal);
		Assert.Contains("/email/track/click/", email.HtmlBody, StringComparison.Ordinal);
		Assert.NotNull(operation);
		Assert.Single(operation!.LinkedEmails);
		Assert.Equal(email.Id, operation.LinkedEmails[0].Id);
	}

	[Fact]
	public async Task GetPlatformTenantsAsync_Returns_Pending_Invitation_Counts_On_Sqlite()
	{
		await using var harness = await TestHarness.CreateAsync();
		const string baseUrl = "https://survey.example.test";

		await harness.TenantAdministrationService.CreateTenantInvitationAsync(new TenantUserInviteModel
		{
			Email = "pending@example.com",
			Role = TenantRole.User
		}, baseUrl);

		var tenants = (await harness.PlatformAdministrationService.GetPlatformTenantsAsync(CreatePagedRequest())).Items;
		var tenant = Assert.Single(tenants);

		Assert.Equal(harness.PrimaryTenantId, tenant.Id);
		Assert.Equal(1, tenant.PendingInvitationCount);
		Assert.Equal(1, tenant.OwnerCount);
	}

	[Fact]
	public async Task AcceptTenantInvitationForNewUserAsync_Creates_A_Membership_In_The_Invited_Tenant()
	{
		await using var harness = await TestHarness.CreateAsync();
		const string baseUrl = "https://survey.example.test";

		var invite = await harness.TenantAdministrationService.CreateTenantInvitationAsync(new TenantUserInviteModel
		{
			Email = "new.user@example.com",
			Role = TenantRole.Admin
		}, baseUrl);

		var userId = await harness.TenantAdministrationService.AcceptTenantInvitationForNewUserAsync(new TenantInvitationRegistrationModel
		{
			Token = invite.Token,
			FirstName = "New",
			LastName = "User",
			Password = "TempPass123!",
			ConfirmPassword = "TempPass123!"
		});

		var membership = await harness.DbContext.TenantMemberships
			.AsNoTracking()
			.SingleAsync(item => item.UserId == userId && item.TenantId == harness.PrimaryTenantId);
		var user = await harness.UserManager.FindByIdAsync(userId);

		Assert.NotNull(user);
		Assert.Equal(TenantRole.Admin, membership.Role);
		Assert.True(membership.IsEnabled);
		Assert.Equal(membership.Id, user!.ActiveTenantMembershipId);
	}

	[Fact]
	public async Task CreateTenantInvitationAsync_Throws_When_Inviting_A_Second_Owner()
	{
		await using var harness = await TestHarness.CreateAsync();
		const string baseUrl = "https://survey.example.test";

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			harness.TenantAdministrationService.CreateTenantInvitationAsync(new TenantUserInviteModel
			{
				Email = "owner2@example.com",
				Role = TenantRole.Owner
			}, baseUrl));

		Assert.Contains("only have one owner", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task CreatePlatformUserInvitationAsync_Queues_Tracked_Email()
	{
		await using var harness = await TestHarness.CreateAsync();
		const string baseUrl = "https://survey.example.test";

		await harness.PlatformAdministrationService.CreatePlatformUserInvitationAsync(new PlatformUserInviteModel
		{
			Email = "platform.invitee@example.com",
			IsPlatformUserEnabled = true,
			TenantId = harness.PrimaryTenantId,
			TenantRole = TenantRole.Admin
		}, baseUrl);

		var invitation = await harness.DbContext.PlatformUserInvitations.SingleAsync(item => item.Email == "platform.invitee@example.com");
		var email = await harness.DbContext.OutboundEmails.SingleAsync(item =>
			item.SourceType == OutboundEmailSourceTypes.PlatformInvitation
			&& item.SourceId == invitation.Id.ToString());

		Assert.Equal("platform.invitee@example.com", email.RecipientEmail);
		Assert.Equal(harness.PrimaryTenantId, email.TenantId);
		Assert.Contains("/email/track/open/", email.HtmlBody, StringComparison.Ordinal);
		Assert.Contains("/email/track/click/", email.HtmlBody, StringComparison.Ordinal);
	}

	[Fact]
	public async Task SendAssignmentEmailAsync_Queues_A_Tracked_Email_For_The_Selected_Location_Email()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();
		const string baseUrl = "https://survey.example.test";

		var result = await harness.TenantAdministrationService.SendAssignmentEmailAsync(seed.AssignmentId, baseUrl);
		var email = await harness.DbContext.OutboundEmails.SingleAsync(item => item.Id == result.OutboundEmailId);
		var operation = await harness.BackgroundOperationsService.GetBackgroundOperationAsync(result.BackgroundOperationId);

		Assert.Equal(OutboundEmailSourceTypes.Assignment, email.SourceType);
		Assert.Equal(seed.AssignmentId.ToString(), email.SourceId);
		Assert.Equal(harness.PrimaryTenantId, email.TenantId);
		Assert.Contains($"/survey/{seed.Token}", email.TextBody, StringComparison.Ordinal);
		Assert.Contains("/email/track/click/", email.HtmlBody, StringComparison.Ordinal);
		Assert.NotNull(operation);
		Assert.Single(operation!.LinkedEmails);
		Assert.Equal(email.Id, operation.LinkedEmails[0].Id);
	}

	[Fact]
	public async Task EmailTrackingService_Records_Open_And_Click_Aggregates_And_Click_Events()
	{
		await using var harness = await TestHarness.CreateAsync();
		var email = new OutboundEmail(
			"identity-confirm-email",
			OutboundEmailSourceTypes.IdentityConfirmation,
			harness.PrimaryUserId,
			"user@example.com",
			"Confirm your email",
			"<p><a href=\"https://survey.example.test/Account/ConfirmEmail?code=abc\">Confirm</a></p>",
			"Confirm your email",
			"track-token-123",
			harness.PrimaryTenantId,
			harness.PrimaryUserId,
			"Test User");
		harness.DbContext.OutboundEmails.Add(email);
		await harness.DbContext.SaveChangesAsync();

		await harness.EmailTrackingService.TrackOpenAsync(email.TrackingToken, "UnitTest", "127.0.0.1");
		var redirect = await harness.EmailTrackingService.TrackClickAsync(
			email.TrackingToken,
			"confirm-email",
			"https://survey.example.test/Account/ConfirmEmail?code=abc",
			"UnitTest",
			"127.0.0.1");

		var reloaded = await harness.DbContext.OutboundEmails
			.Include(item => item.ClickEvents)
			.SingleAsync(item => item.Id == email.Id);

		Assert.True(redirect.IsValid);
		Assert.Equal("https://survey.example.test/Account/ConfirmEmail?code=abc", redirect.DestinationUrl);
		Assert.Equal(1, reloaded.OpenCount);
		Assert.NotNull(reloaded.FirstOpenedUtc);
		Assert.NotNull(reloaded.LastOpenedUtc);
		Assert.Equal(1, reloaded.ClickCount);
		Assert.NotNull(reloaded.FirstClickedUtc);
		Assert.NotNull(reloaded.LastClickedUtc);
		var clickEvent = Assert.Single(reloaded.ClickEvents);
		Assert.Equal("confirm-email", clickEvent.LinkType);
		Assert.Equal("https://survey.example.test/Account/ConfirmEmail?code=abc", clickEvent.DestinationUrl);
		Assert.False(string.IsNullOrWhiteSpace(clickEvent.IpAddressHash));
	}

	[Fact]
	public async Task InitializeSurveyPlatformAsync_Can_Bootstrap_A_Fresh_Sqlite_Database()
	{
		var databasePath = Path.Combine(Path.GetTempPath(), $"survey-fresh-init-{Guid.NewGuid():N}.db");
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
		services.AddSingleton<IConfiguration>(configuration);
		services.AddSurveyInfrastructure(configuration);

		var provider = services.BuildServiceProvider();

		try
		{
			await provider.InitializeSurveyPlatformAsync();

			await using var scope = provider.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<SurveyDbContext>();
			await dbContext.Database.OpenConnectionAsync();
			await using var command = dbContext.Database.GetDbConnection().CreateCommand();
			command.CommandText = """SELECT COUNT(*) FROM "sqlite_master" WHERE "type" = 'table' AND "name" = 'PostalAddresses';""";

			Assert.True(await dbContext.Database.CanConnectAsync());
			Assert.Equal(1L, (long)(await command.ExecuteScalarAsync() ?? 0L));
		}
		finally
		{
			await provider.DisposeAsync();
			TryDeleteSqliteArtifacts(databasePath);
		}
	}

	[Fact]
	public async Task InitializeSurveyPlatformAsync_Does_Not_Seed_Geography_Reference_Data()
	{
		var databasePath = Path.Combine(Path.GetTempPath(), $"survey-no-geo-seed-{Guid.NewGuid():N}.db");
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
		services.AddSingleton<IConfiguration>(configuration);
		services.AddSurveyInfrastructure(configuration);

		var provider = services.BuildServiceProvider();

		try
		{
			await provider.InitializeSurveyPlatformAsync();

			await using var scope = provider.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<SurveyDbContext>();

			Assert.Empty(await dbContext.Countries.ToListAsync());
			Assert.Empty(await dbContext.StateProvinces.ToListAsync());
			Assert.Empty(await dbContext.Counties.ToListAsync());
			Assert.Empty(await dbContext.ZipCountyLookups.ToListAsync());
		}
		finally
		{
			await provider.DisposeAsync();
			TryDeleteSqliteArtifacts(databasePath);
		}
	}

	[Fact]
	public async Task InitializeSurveyPlatformAsync_Does_Not_Add_Existing_Users_To_Another_Users_Tenant_When_Memberships_Already_Exist()
	{
		var databasePath = Path.Combine(Path.GetTempPath(), $"survey-bootstrap-{Guid.NewGuid():N}.db");
		var inviterUserId = string.Empty;
		var inviteeUserId = string.Empty;
		var inviterTenantId = 0;
		var inviteeTenantId = 0;
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
		services.AddSingleton<IConfiguration>(configuration);
		services.AddSurveyInfrastructure(configuration);

		var provider = services.BuildServiceProvider();

		try
		{
			await provider.InitializeSurveyPlatformAsync();

			await using (var scope = provider.CreateAsyncScope())
			{
				var dbContext = scope.ServiceProvider.GetRequiredService<SurveyDbContext>();
				var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

				var inviteeTenant = new Tenant("Invitee Tenant");
				var inviterTenant = new Tenant("Inviter Tenant");
				inviteeTenantId = inviteeTenant.Id;
				inviterTenantId = inviterTenant.Id;
				dbContext.Tenants.AddRange(inviteeTenant, inviterTenant);
				await dbContext.SaveChangesAsync();
				inviteeTenantId = inviteeTenant.Id;
				inviterTenantId = inviterTenant.Id;

				var inviter = new ApplicationUser
				{
					UserName = "inviter@example.com",
					Email = "inviter@example.com",
					EmailConfirmed = true
				};
				UserAvatarPalette.EnsureAssigned(inviter);

				var invitee = new ApplicationUser
				{
					UserName = "invitee@example.com",
					Email = "invitee@example.com",
					EmailConfirmed = true
				};
				UserAvatarPalette.EnsureAssigned(invitee);

				var inviterResult = await userManager.CreateAsync(inviter, "TempPass123!");
				Assert.True(inviterResult.Succeeded, string.Join("; ", inviterResult.Errors.Select(static error => error.Description)));

				var inviteeResult = await userManager.CreateAsync(invitee, "TempPass123!");
				Assert.True(inviteeResult.Succeeded, string.Join("; ", inviteeResult.Errors.Select(static error => error.Description)));
				inviterUserId = inviter.Id;
				inviteeUserId = invitee.Id;

				var inviterMembership = new TenantMembership(inviterTenant.Id, inviter.Id, TenantRole.Owner);
				var inviteeOwnerMembership = new TenantMembership(inviteeTenant.Id, invitee.Id, TenantRole.Owner);
				var acceptedInviteMembership = new TenantMembership(inviterTenant.Id, invitee.Id, TenantRole.User);
				dbContext.TenantMemberships.AddRange(inviterMembership, inviteeOwnerMembership, acceptedInviteMembership);
				await dbContext.SaveChangesAsync();

				inviter.ActiveTenantMembershipId = inviterMembership.Id;
				invitee.ActiveTenantMembershipId = acceptedInviteMembership.Id;
				await userManager.UpdateAsync(inviter);
				await userManager.UpdateAsync(invitee);
			}

			await provider.InitializeSurveyPlatformAsync();

			await using var verificationScope = provider.CreateAsyncScope();
			var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<SurveyDbContext>();
			var membershipRows = await verificationDbContext.TenantMemberships
				.AsNoTracking()
				.OrderBy(membership => membership.UserId)
				.ThenBy(membership => membership.TenantId)
				.Select(membership => new { membership.UserId, membership.TenantId, membership.Role })
				.ToListAsync();

			var groupedMemberships = membershipRows
				.GroupBy(membership => membership.UserId, StringComparer.Ordinal)
				.ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

			Assert.Collection(
				groupedMemberships[inviterUserId],
				membership =>
				{
					Assert.Equal(inviterTenantId, membership.TenantId);
					Assert.Equal(TenantRole.Owner, membership.Role);
				});
			Assert.Collection(
				groupedMemberships[inviteeUserId],
				firstMembership =>
				{
					Assert.Equal(inviteeTenantId, firstMembership.TenantId);
					Assert.Equal(TenantRole.Owner, firstMembership.Role);
				},
				secondMembership =>
				{
					Assert.Equal(inviterTenantId, secondMembership.TenantId);
					Assert.Equal(TenantRole.User, secondMembership.Role);
				});
		}
		finally
		{
			await provider.DisposeAsync();
			TryDeleteSqliteArtifacts(databasePath);
		}
	}

	private static void TryDeleteSqliteArtifacts(string databasePath)
	{
		foreach (var path in new[] { databasePath, $"{databasePath}-wal", $"{databasePath}-shm" })
		{
			try
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}

	[Fact]
	public async Task SaveTenantUserAsync_Throws_When_Current_User_Tries_To_Change_Their_Own_Access()
	{
		await using var harness = await TestHarness.CreateAsync();

		var model = await harness.TenantAdministrationService.GetTenantUserAsync(harness.PrimaryMembershipId);
		model.IsEnabled = false;

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.TenantAdministrationService.SaveTenantUserAsync(model));

		Assert.Contains("cannot change your own tenant role, status, or permissions", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task TenantContextAccessor_Uses_HttpContext_User_When_Component_Auth_State_Is_Unavailable()
	{
		await using var harness = await TestHarness.CreateAsync();

		var user = await harness.UserManager.FindByIdAsync(harness.PrimaryUserId);
		Assert.NotNull(user);

		harness.HttpContextAccessor.HttpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(
			[
				new Claim(ClaimTypes.NameIdentifier, harness.PrimaryUserId),
				new Claim(ClaimTypes.Name, user!.UserName ?? user.Email ?? harness.PrimaryUserId),
				new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
			], "TestHttpContext"))
		};

		var accessor = new TenantContextAccessor(
			new ThrowingAuthenticationStateProvider(),
			harness.HttpContextAccessor,
			harness.UserManager,
			harness.DbContext,
			harness.TenantExecutionContext);

		var context = await accessor.GetCurrentAsync();

		Assert.True(context.IsAuthenticated);
		Assert.True(context.HasTenantAccess);
		Assert.Equal(harness.PrimaryUserId, context.UserId);
		Assert.Equal(harness.PrimaryTenantId, context.TenantId);
		Assert.Equal(harness.PrimaryMembershipId, context.ActiveTenantMembershipId);
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

		var active = (await harness.AdministrationService.GetSurveyDefinitionsAsync(CreatePagedRequest())).Items;
		var archived = (await harness.AdministrationService.GetSurveyDefinitionsAsync(CreatePagedRequest(), true)).Items;

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

		var active = (await harness.AdministrationService.GetSurveyVersionsAsync(CreatePagedRequest(), seed.DefinitionId)).Items;
		var archived = (await harness.AdministrationService.GetSurveyVersionsAsync(CreatePagedRequest(), seed.DefinitionId, true)).Items;

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

		var active = (await harness.AdministrationService.GetSurveyVersionsAsync(CreatePagedRequest(), definitionId)).Items;
		var archived = (await harness.AdministrationService.GetSurveyVersionsAsync(CreatePagedRequest(), definitionId, true)).Items;

		Assert.Empty(active);
		Assert.Single(archived);
		Assert.True(archived[0].IsArchived);
	}

	[Fact]
	public async Task GetStateProvincesAsync_Returns_CountyCounts_When_Filtered_By_Country()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		var states = (await harness.PlatformAdministrationService.GetStateProvincesAsync(CreatePagedRequest(), seed.CountryId)).Items;

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

		var assignments = (await harness.AdministrationService.GetAssignmentsAsync(CreatePagedRequest())).Items;

		Assert.Equal([laterAssignment.Id, earlierAssignment.Id], assignments.Select(assignment => assignment.Id).Take(2).ToArray());
	}

	[Fact]
	public async Task ImportZipCountyMappingsAsync_And_Goal_Report_Work_Together()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		await harness.PlatformAdministrationService.ImportZipCountyMappingsAsync(new ZipCountyImportModel
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

		var states = (await harness.PlatformAdministrationService.GetStateProvincesAsync(CreatePagedRequest(), seed.CountryId)).Items;
		var state = Assert.Single(states);
		Assert.Equal(2, state.CountyCount);

		var counties = (await harness.PlatformAdministrationService.GetCountiesAsync(CreatePagedRequest(), seed.StateProvinceId)).Items;
		Assert.Equal(2, counties.Count);
		Assert.All(counties, county => Assert.Equal("Florida (FL)", county.StateProvinceFilterName));

		var addresses = (await harness.PlatformAdministrationService.GetPostalAddressesAsync(CreatePagedRequest(), countyId: seed.MiamiDadeCountyId)).Items;
		var address = Assert.Single(addresses);
		Assert.Equal(seed.MiamiDadeCountyId, address.CountyId);
		Assert.Equal("Miami-Dade County", address.CountyName);

		var areas = (await harness.AdministrationService.GetAreasAsync(CreatePagedRequest(), seed.OrangeCountyId)).Items;
		var area = Assert.Single(areas);
		Assert.Equal("Central Florida", area.Name);
		Assert.Equal("Orange County", area.CountyNameFilter);
	}

	[Fact]
	public async Task SaveGoalAsync_Allows_All_Areas_And_Counts_Responses_Across_Counties()
	{
		await using var harness = await TestHarness.CreateAsync();
		var seed = await harness.SeedSurveyAsync();

		await harness.PlatformAdministrationService.ImportZipCountyMappingsAsync(new ZipCountyImportModel
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

		var goals = (await harness.AdministrationService.GetGoalsAsync(CreatePagedRequest())).Items;
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

		var responses = (await harness.AdministrationService.GetResponsesAsync(CreatePagedRequest())).Items;

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
			TenantAdministrationService = _scope.ServiceProvider.GetRequiredService<ITenantAdministrationService>();
			PlatformAdministrationService = _scope.ServiceProvider.GetRequiredService<IPlatformAdministrationService>();
			AdministrationService = _scope.ServiceProvider.GetRequiredService<ITenantAdministrationService>();
			ExperienceService = _scope.ServiceProvider.GetRequiredService<ISurveyExperienceService>();
			UserManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
			TenantExecutionContext = _scope.ServiceProvider.GetRequiredService<TenantExecutionContext>();
			AuthenticationStateProvider = (TestAuthenticationStateProvider)_scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();
			HttpContextAccessor = _scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
			BackgroundOperationsService = _scope.ServiceProvider.GetRequiredService<IBackgroundOperationsService>();
			EmailTrackingService = _scope.ServiceProvider.GetRequiredService<IEmailTrackingService>();
			GeographySeeder = _scope.ServiceProvider.GetRequiredService<GeographyDataSeeder>();
			InitialSetupSeeder = _scope.ServiceProvider.GetRequiredService<InitialSetupSeeder>();
		}

		public SurveyDbContext DbContext { get; }
		public ITenantAdministrationService TenantAdministrationService { get; }
		public IPlatformAdministrationService PlatformAdministrationService { get; }
		public ITenantAdministrationService AdministrationService { get; }
		public ISurveyExperienceService ExperienceService { get; }
		public UserManager<ApplicationUser> UserManager { get; }
		public TenantExecutionContext TenantExecutionContext { get; }
		public TestAuthenticationStateProvider AuthenticationStateProvider { get; }
		public IHttpContextAccessor HttpContextAccessor { get; }
		public IBackgroundOperationsService BackgroundOperationsService { get; }
		public IEmailTrackingService EmailTrackingService { get; }
		public GeographyDataSeeder GeographySeeder { get; }
		public InitialSetupSeeder InitialSetupSeeder { get; }
		public int PrimaryTenantId { get; private set; }
		public int PrimaryMembershipId { get; private set; }
		public string PrimaryUserId { get; private set; } = string.Empty;

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
			services.AddScoped<AuthenticationStateProvider, TestAuthenticationStateProvider>();

			var provider = services.BuildServiceProvider();
			await using (var setupScope = provider.CreateAsyncScope())
			{
				var dbContext = setupScope.ServiceProvider.GetRequiredService<SurveyDbContext>();
				await dbContext.Database.EnsureDeletedAsync();
				await dbContext.Database.EnsureCreatedAsync();
			}

			var scope = provider.CreateScope();
			var harness = new TestHarness(provider, scope, databasePath);
			await harness.InitializeAccessAsync();

			return harness;
		}

		private async Task InitializeAccessAsync()
		{
			var tenant = new Tenant("Test Tenant");
			DbContext.Tenants.Add(tenant);
			await DbContext.SaveChangesAsync();

			var user = new ApplicationUser
			{
				UserName = "admin@example.com",
				Email = "admin@example.com",
				EmailConfirmed = true,
				FirstName = "Test",
				LastName = "Admin",
				IsPlatformSuperAdmin = true,
				IsPlatformUserEnabled = true
			};

			var createResult = await UserManager.CreateAsync(user, "TempPass123!");
			if (!createResult.Succeeded)
			{
				throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(static error => error.Description)));
			}

			var membership = new TenantMembership(tenant.Id, user.Id, TenantRole.Owner);
			DbContext.TenantMemberships.Add(membership);
			await DbContext.SaveChangesAsync();

			PrimaryTenantId = tenant.Id;
			PrimaryMembershipId = membership.Id;
			PrimaryUserId = user.Id;
			await SetCurrentUserAsync(user, membership.Id);
		}

		public Task CompleteInitialSetupAsync(string defaultThemeKey, params string[] additionalThemeKeys)
		{
			var selectedThemeKeys = additionalThemeKeys
				.Concat([defaultThemeKey])
				.Where(static key => !string.IsNullOrWhiteSpace(key))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();

			return InitialSetupSeeder.SeedAsync(selectedThemeKeys, defaultThemeKey, cancellationToken: CancellationToken.None);
		}

		public async Task SetCurrentUserAsync(ApplicationUser user, int? membershipId = null)
		{
			user.ActiveTenantMembershipId = membershipId;
			var updateResult = await UserManager.UpdateAsync(user);
			if (!updateResult.Succeeded)
			{
				throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(static error => error.Description)));
			}

			if (membershipId.HasValue)
			{
				var tenantId = await DbContext.TenantMemberships
					.AsNoTracking()
					.Where(item => item.Id == membershipId.Value)
					.Select(item => item.TenantId)
					.SingleAsync();
				TenantExecutionContext.UseTenant(tenantId);
			}
			else
			{
				TenantExecutionContext.Clear();
			}

			AuthenticationStateProvider.SetPrincipal(new ClaimsPrincipal(new ClaimsIdentity(
			[
				new Claim(ClaimTypes.NameIdentifier, user.Id),
				new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
				new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
			], "TestAuth")));
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

	private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
	{
		private ClaimsPrincipal _principal = new(new ClaimsIdentity());

		public override Task<AuthenticationState> GetAuthenticationStateAsync()
		{
			return Task.FromResult(new AuthenticationState(_principal));
		}

		public void SetPrincipal(ClaimsPrincipal principal)
		{
			_principal = principal;
			NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
		}
	}

	private sealed class ThrowingAuthenticationStateProvider : AuthenticationStateProvider
	{
		public override Task<AuthenticationState> GetAuthenticationStateAsync()
		{
			throw new InvalidOperationException("Authentication state is unavailable in this scope.");
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
