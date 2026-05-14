using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Survey.Domain;
using Survey.Infrastructure.Identity;

namespace Survey.Infrastructure.Persistence;

public class SurveyDbContext(DbContextOptions<SurveyDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
	public DbSet<Country> Countries => Set<Country>();
	public DbSet<StateProvince> StateProvinces => Set<StateProvince>();
	public DbSet<County> Counties => Set<County>();
	public DbSet<PostalAddress> PostalAddresses => Set<PostalAddress>();
	public DbSet<Person> People => Set<Person>();
	public DbSet<Area> Areas => Set<Area>();
	public DbSet<AreaCounty> AreaCounties => Set<AreaCounty>();
	public DbSet<Goal> Goals => Set<Goal>();
	public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
	public DbSet<SurveyDefinition> SurveyDefinitions => Set<SurveyDefinition>();
	public DbSet<SurveyVersion> SurveyVersions => Set<SurveyVersion>();
	public DbSet<SurveySection> SurveySections => Set<SurveySection>();
	public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();
	public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
	public DbSet<SurveyAssignment> SurveyAssignments => Set<SurveyAssignment>();
	public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
	public DbSet<SurveyAnswer> SurveyAnswers => Set<SurveyAnswer>();
	public DbSet<ZipCountyLookup> ZipCountyLookups => Set<ZipCountyLookup>();

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		builder.Entity<ApplicationUser>(entity =>
		{
			entity.Property(user => user.FirstName).HasMaxLength(100);
			entity.Property(user => user.LastName).HasMaxLength(100);
			entity.Property(user => user.FavoriteGoalIds).HasMaxLength(2000);
		});

		builder.Entity<Country>(entity =>
		{
			entity.Property(country => country.Name).HasMaxLength(200).IsRequired();
			entity.Property(country => country.Iso2Code).HasMaxLength(2).IsRequired();
			entity.Property(country => country.Iso3Code).HasMaxLength(3);
			entity.HasIndex(country => country.Name).IsUnique();
			entity.HasIndex(country => country.Iso2Code).IsUnique();
		});

		builder.Entity<StateProvince>(entity =>
		{
			entity.Property(stateProvince => stateProvince.Name).HasMaxLength(200).IsRequired();
			entity.Property(stateProvince => stateProvince.Code).HasMaxLength(20).IsRequired();
			entity.Property(stateProvince => stateProvince.SubdivisionType).HasMaxLength(50).IsRequired();
			entity.HasIndex(stateProvince => new { stateProvince.CountryId, stateProvince.Name }).IsUnique();
			entity.HasIndex(stateProvince => new { stateProvince.CountryId, stateProvince.Code }).IsUnique();
			entity.HasOne(stateProvince => stateProvince.Country)
				.WithMany(country => country.StateProvinces)
				.HasForeignKey(stateProvince => stateProvince.CountryId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<County>(entity =>
		{
			entity.Property(county => county.Name).HasMaxLength(200).IsRequired();
			entity.Property(county => county.FipsCode).HasMaxLength(20).IsRequired();
			entity.HasIndex(county => new { county.StateProvinceId, county.Name }).IsUnique();
			entity.HasIndex(county => new { county.StateProvinceId, county.FipsCode }).IsUnique();
			entity.HasOne(county => county.StateProvince)
				.WithMany(stateProvince => stateProvince.Counties)
				.HasForeignKey(county => county.StateProvinceId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<PostalAddress>(entity =>
		{
			entity.Property(address => address.AddressLine1).HasMaxLength(200).IsRequired();
			entity.Property(address => address.AddressLine2).HasMaxLength(200);
			entity.Property(address => address.City).HasMaxLength(100).IsRequired();
			entity.Property(address => address.PostalCode).HasMaxLength(20).IsRequired();
			entity.Property(address => address.FormattedAddress).HasMaxLength(500).IsRequired();
			entity.Property(address => address.NormalizedKey).HasMaxLength(1000).IsRequired();
			entity.HasIndex(address => address.NormalizedKey).IsUnique();
			entity.HasOne(address => address.Country)
				.WithMany(country => country.PostalAddresses)
				.HasForeignKey(address => address.CountryId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(address => address.StateProvince)
				.WithMany(stateProvince => stateProvince.PostalAddresses)
				.HasForeignKey(address => address.StateProvinceId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(address => address.County)
				.WithMany(county => county.PostalAddresses)
				.HasForeignKey(address => address.CountyId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<Person>(entity =>
		{
			entity.Property(person => person.FirstName).HasMaxLength(100).IsRequired();
			entity.Property(person => person.MiddleName).HasMaxLength(100);
			entity.Property(person => person.PostalAddressId);
			entity.Property(person => person.LastName).HasMaxLength(100).IsRequired();
			entity.Property(person => person.AddressLine1).HasMaxLength(200);
			entity.Property(person => person.AddressLine2).HasMaxLength(200);
			entity.Property(person => person.City).HasMaxLength(100);
			entity.Property(person => person.State).HasMaxLength(100);
			entity.Property(person => person.HomeAddress).HasMaxLength(500).IsRequired();
			entity.Property(person => person.PostalCode).HasMaxLength(20);
			entity.Property(person => person.PhoneNumber).HasMaxLength(50).IsRequired();
			entity.Property(person => person.BestTimeToContact).HasMaxLength(100);
			entity.Property(person => person.Email).HasMaxLength(256).IsRequired();
			entity.HasIndex(person => person.Email);
			entity.HasOne(person => person.PostalAddress)
				.WithMany(address => address.People)
				.HasForeignKey(person => person.PostalAddressId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<Area>(entity =>
		{
			entity.Property(area => area.Name).HasMaxLength(200).IsRequired();
			entity.Property(area => area.Description).HasMaxLength(2000);
		});

		builder.Entity<AreaCounty>(entity =>
		{
			entity.Property(county => county.CountyFips).HasMaxLength(5).IsRequired();
			entity.Property(county => county.CountyName).HasMaxLength(200).IsRequired();
			entity.Property(county => county.StateCode).HasMaxLength(2).IsRequired();
			entity.HasIndex(county => new { county.AreaId, county.CountyFips }).IsUnique();
			entity.HasOne(county => county.Area)
				.WithMany(area => area.Counties)
				.HasForeignKey(county => county.AreaId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<SurveyDefinition>(entity =>
		{
			entity.Property(definition => definition.Name).HasMaxLength(200).IsRequired();
			entity.Property(definition => definition.Description).HasMaxLength(2000);
			entity.Property(definition => definition.IsArchived).HasDefaultValue(false);
		});

		builder.Entity<Goal>(entity =>
		{
			entity.Property(goal => goal.Name).HasMaxLength(200).IsRequired();
			entity.Property(goal => goal.Description).HasMaxLength(2000);
			entity.HasOne(goal => goal.Area)
				.WithMany(area => area.Goals)
				.HasForeignKey(goal => goal.AreaId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(goal => goal.SurveyDefinition)
				.WithMany(definition => definition.Goals)
				.HasForeignKey(goal => goal.SurveyDefinitionId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<SiteSetting>(entity =>
		{
			entity.Property(setting => setting.Id).ValueGeneratedNever();
			entity.Property(setting => setting.ThemePresetKey).HasMaxLength(100).IsRequired();
		});

		builder.Entity<SurveyVersion>(entity =>
		{
			entity.Property(version => version.DisplayName).HasMaxLength(200).IsRequired();
			entity.Property(version => version.IsArchived).HasDefaultValue(false);
			entity.HasIndex(version => new { version.SurveyDefinitionId, version.VersionNumber }).IsUnique();
			entity.HasOne(version => version.SurveyDefinition)
				.WithMany(definition => definition.Versions)
				.HasForeignKey(version => version.SurveyDefinitionId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<SurveySection>(entity =>
		{
			entity.Property(section => section.Title).HasMaxLength(200).IsRequired();
			entity.Property(section => section.Description).HasMaxLength(1000);
			entity.HasOne(section => section.SurveyVersion)
				.WithMany(version => version.Sections)
				.HasForeignKey(section => section.SurveyVersionId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<SurveyQuestion>(entity =>
		{
			entity.Property(question => question.Prompt).HasMaxLength(2000).IsRequired();
			entity.Property(question => question.HelpText).HasMaxLength(1000);
			entity.HasOne(question => question.SurveySection)
				.WithMany(section => section.Questions)
				.HasForeignKey(question => question.SurveySectionId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<QuestionOption>(entity =>
		{
			entity.Property(option => option.Label).HasMaxLength(200).IsRequired();
			entity.HasOne(option => option.SurveyQuestion)
				.WithMany(question => question.Options)
				.HasForeignKey(option => option.SurveyQuestionId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<SurveyAssignment>(entity =>
		{
			entity.Property(assignment => assignment.PublicToken).HasMaxLength(100).IsRequired();
			entity.Property(assignment => assignment.CreatedByUserId).HasMaxLength(450);
			entity.HasIndex(assignment => assignment.PublicToken).IsUnique();
			entity.HasOne(assignment => assignment.Person)
				.WithMany(person => person.Assignments)
				.HasForeignKey(assignment => assignment.PersonId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(assignment => assignment.SurveyVersion)
				.WithMany(version => version.Assignments)
				.HasForeignKey(assignment => assignment.SurveyVersionId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<SurveyResponse>(entity =>
		{
			entity.Property(response => response.SubmittedByUserId).HasMaxLength(450);
			entity.Property(response => response.RespondentFirstName).HasMaxLength(100).IsRequired();
			entity.Property(response => response.RespondentMiddleName).HasMaxLength(100);
			entity.Property(response => response.RespondentLastName).HasMaxLength(100).IsRequired();
			entity.Property(response => response.RespondentPostalAddressId);
			entity.Property(response => response.RespondentAddressLine1).HasMaxLength(200);
			entity.Property(response => response.RespondentAddressLine2).HasMaxLength(200);
			entity.Property(response => response.RespondentCity).HasMaxLength(100);
			entity.Property(response => response.RespondentState).HasMaxLength(100);
			entity.Property(response => response.RespondentHomeAddress).HasMaxLength(500).IsRequired();
			entity.Property(response => response.RespondentPostalCode).HasMaxLength(20);
			entity.Property(response => response.RespondentCountyFipsSnapshot).HasMaxLength(5);
			entity.Property(response => response.RespondentCountyNameSnapshot).HasMaxLength(200);
			entity.Property(response => response.RespondentStateCodeSnapshot).HasMaxLength(2);
			entity.Property(response => response.RespondentPhoneNumber).HasMaxLength(50).IsRequired();
			entity.Property(response => response.RespondentBestTimeToContact).HasMaxLength(100);
			entity.Property(response => response.RespondentEmail).HasMaxLength(256).IsRequired();
			entity.Property(response => response.SurveyNameSnapshot).HasMaxLength(200).IsRequired();
			entity.Property(response => response.SurveyVersionNameSnapshot).HasMaxLength(200).IsRequired();
			entity.HasIndex(response => response.SurveyAssignmentId).IsUnique();
			entity.HasOne(response => response.SurveyAssignment)
				.WithOne(assignment => assignment.Response)
				.HasForeignKey<SurveyResponse>(response => response.SurveyAssignmentId)
				.OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(response => response.RespondentPostalAddress)
				.WithMany(address => address.SurveyResponses)
				.HasForeignKey(response => response.RespondentPostalAddressId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<SurveyAnswer>(entity =>
		{
			entity.Property(answer => answer.QuestionPromptSnapshot).HasMaxLength(2000).IsRequired();
			entity.HasOne(answer => answer.SurveyResponse)
				.WithMany(response => response.Answers)
				.HasForeignKey(answer => answer.SurveyResponseId)
				.OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(answer => answer.SurveyQuestion)
				.WithMany()
				.HasForeignKey(answer => answer.SurveyQuestionId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<ZipCountyLookup>(entity =>
		{
			entity.Property(mapping => mapping.ZipCode).HasMaxLength(10).IsRequired();
			entity.Property(mapping => mapping.CountyFips).HasMaxLength(5).IsRequired();
			entity.Property(mapping => mapping.CountyName).HasMaxLength(200).IsRequired();
			entity.Property(mapping => mapping.StateCode).HasMaxLength(2).IsRequired();
			entity.Property(mapping => mapping.ResidentialRatio).HasPrecision(9, 6);
			entity.HasIndex(mapping => new { mapping.ZipCode, mapping.CountyFips }).IsUnique();
			entity.HasIndex(mapping => mapping.ZipCode);
		});

		builder.Entity<IdentityRole>(entity =>
		{
			entity.Property(role => role.Name).HasMaxLength(256);
			entity.Property(role => role.NormalizedName).HasMaxLength(256);
		});
	}
}
