using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Survey.Domain;
using Survey.Infrastructure.Identity;
using Survey.Infrastructure.Security;

namespace Survey.Infrastructure.Persistence;

public class SurveyDbContext(
	DbContextOptions<SurveyDbContext> options,
	TenantExecutionContext tenantExecutionContext) : IdentityDbContext<ApplicationUser>(options)
{
	private readonly TenantExecutionContext _tenantExecutionContext = tenantExecutionContext;
	private int CurrentTenantId => _tenantExecutionContext.TenantId ?? 0;
	private bool HasTenantContext => _tenantExecutionContext.TenantId.HasValue;
	private bool BypassTenantIsolation => _tenantExecutionContext.BypassTenantIsolation;

	public DbSet<Tenant> Tenants => Set<Tenant>();
	public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
	public DbSet<TenantMembershipPermission> TenantMembershipPermissions => Set<TenantMembershipPermission>();
	public DbSet<TenantInvitation> TenantInvitations => Set<TenantInvitation>();
	public DbSet<TenantSetting> TenantSettings => Set<TenantSetting>();
	public DbSet<TenantVisibleCountry> TenantVisibleCountries => Set<TenantVisibleCountry>();
	public DbSet<TenantVisibleStateProvince> TenantVisibleStateProvinces => Set<TenantVisibleStateProvince>();
	public DbSet<TenantVisibleCounty> TenantVisibleCounties => Set<TenantVisibleCounty>();
	public DbSet<PlatformUserPermission> PlatformUserPermissions => Set<PlatformUserPermission>();
	public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
	public DbSet<Country> Countries => Set<Country>();
	public DbSet<StateProvince> StateProvinces => Set<StateProvince>();
	public DbSet<County> Counties => Set<County>();
	public DbSet<PostalAddress> PostalAddresses => Set<PostalAddress>();
	public DbSet<Person> People => Set<Person>();
	public DbSet<PersonPhone> PersonPhones => Set<PersonPhone>();
	public DbSet<PersonEmail> PersonEmails => Set<PersonEmail>();
	public DbSet<Location> Locations => Set<Location>();
	public DbSet<LocationPhone> LocationPhones => Set<LocationPhone>();
	public DbSet<LocationEmail> LocationEmails => Set<LocationEmail>();
	public DbSet<Area> Areas => Set<Area>();
	public DbSet<AreaCounty> AreaCounties => Set<AreaCounty>();
	public DbSet<Goal> Goals => Set<Goal>();
	public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
	public DbSet<SeedState> SeedStates => Set<SeedState>();
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
			entity.Property(user => user.AddressLine1).HasMaxLength(200);
			entity.Property(user => user.AddressLine2).HasMaxLength(200);
			entity.Property(user => user.City).HasMaxLength(100);
			entity.Property(user => user.State).HasMaxLength(100);
			entity.Property(user => user.PostalCode).HasMaxLength(20);
			entity.Property(user => user.PhoneNumber).HasMaxLength(50);
			entity.Property(user => user.ActiveTenantMembershipId);
			entity.Property(user => user.AvatarColorHex).HasMaxLength(16);
			entity.HasMany(user => user.TenantMemberships)
				.WithOne()
				.HasForeignKey(membership => membership.UserId)
				.OnDelete(DeleteBehavior.Cascade);
			entity.HasMany(user => user.PlatformPermissions)
				.WithOne()
				.HasForeignKey(permission => permission.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<Tenant>(entity =>
		{
			entity.Property(tenant => tenant.Name).HasMaxLength(200).IsRequired();
			entity.Property(tenant => tenant.Slug).HasMaxLength(200).IsRequired();
			entity.HasIndex(tenant => tenant.Slug).IsUnique();
		});

		builder.Entity<TenantMembership>(entity =>
		{
			entity.Property(membership => membership.UserId).HasMaxLength(450).IsRequired();
			entity.HasIndex(membership => new { membership.TenantId, membership.UserId }).IsUnique();
			entity.HasIndex(membership => membership.UserId);
			entity.HasOne(membership => membership.Tenant)
				.WithMany(tenant => tenant.Memberships)
				.HasForeignKey(membership => membership.TenantId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<TenantMembershipPermission>(entity =>
		{
			entity.Property(permission => permission.PermissionKey).HasMaxLength(200).IsRequired();
			entity.HasIndex(permission => new { permission.TenantMembershipId, permission.PermissionKey }).IsUnique();
			entity.HasOne(permission => permission.Membership)
				.WithMany(membership => membership.PermissionOverrides)
				.HasForeignKey(permission => permission.TenantMembershipId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<TenantInvitation>(entity =>
		{
			entity.Property(invitation => invitation.Email).HasMaxLength(256).IsRequired();
			entity.Property(invitation => invitation.TokenHash).HasMaxLength(512).IsRequired();
			entity.Property(invitation => invitation.CreatedByUserId).HasMaxLength(450).IsRequired();
			entity.HasIndex(invitation => invitation.TokenHash).IsUnique();
			entity.HasIndex(invitation => new { invitation.TenantId, invitation.Email });
			entity.HasOne(invitation => invitation.Tenant)
				.WithMany(tenant => tenant.Invitations)
				.HasForeignKey(invitation => invitation.TenantId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<TenantSetting>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(setting => setting.ThemePresetKey).HasMaxLength(100).IsRequired();
			entity.HasIndex(setting => setting.TenantId).IsUnique();
			entity.HasOne(setting => setting.Tenant)
				.WithMany(tenant => tenant.Settings)
				.HasForeignKey(setting => setting.TenantId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<TenantVisibleCountry>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.HasIndex(item => new { item.TenantId, item.CountryId }).IsUnique();
			entity.HasOne(item => item.Tenant)
				.WithMany(tenant => tenant.VisibleCountries)
				.HasForeignKey(item => item.TenantId)
				.OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(item => item.Country)
				.WithMany(country => country.TenantVisibility)
				.HasForeignKey(item => item.CountryId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<TenantVisibleStateProvince>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.HasIndex(item => new { item.TenantId, item.StateProvinceId }).IsUnique();
			entity.HasOne(item => item.Tenant)
				.WithMany(tenant => tenant.VisibleStateProvinces)
				.HasForeignKey(item => item.TenantId)
				.OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(item => item.StateProvince)
				.WithMany(stateProvince => stateProvince.TenantVisibility)
				.HasForeignKey(item => item.StateProvinceId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<TenantVisibleCounty>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.HasIndex(item => new { item.TenantId, item.CountyId }).IsUnique();
			entity.HasOne(item => item.Tenant)
				.WithMany(tenant => tenant.VisibleCounties)
				.HasForeignKey(item => item.TenantId)
				.OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(item => item.County)
				.WithMany(county => county.TenantVisibility)
				.HasForeignKey(item => item.CountyId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<PlatformUserPermission>(entity =>
		{
			entity.Property(permission => permission.UserId).HasMaxLength(450).IsRequired();
			entity.Property(permission => permission.PermissionKey).HasMaxLength(200).IsRequired();
			entity.HasIndex(permission => new { permission.UserId, permission.PermissionKey }).IsUnique();
		});

		builder.Entity<AuditLog>(entity =>
		{
			entity.Property(log => log.ActorUserId).HasMaxLength(450);
			entity.Property(log => log.Plane).HasMaxLength(50).IsRequired();
			entity.Property(log => log.ActionType).HasMaxLength(200).IsRequired();
			entity.Property(log => log.TargetType).HasMaxLength(200).IsRequired();
			entity.Property(log => log.TargetId).HasMaxLength(200);
			entity.Property(log => log.Details).HasMaxLength(4000);
			entity.HasIndex(log => new { log.TenantId, log.CreatedUtc });
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
			ConfigureTenantOwnedEntity(entity);
			entity.Property(address => address.AddressLine1).HasMaxLength(200).IsRequired();
			entity.Property(address => address.AddressLine2).HasMaxLength(200);
			entity.Property(address => address.City).HasMaxLength(100).IsRequired();
			entity.Property(address => address.PostalCode).HasMaxLength(20).IsRequired();
			entity.Property(address => address.FormattedAddress).HasMaxLength(500).IsRequired();
			entity.Property(address => address.NormalizedKey).HasMaxLength(1000).IsRequired();
			entity.HasIndex(address => new { address.TenantId, address.NormalizedKey }).IsUnique();
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
			ConfigureTenantOwnedEntity(entity);
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
			entity.Property(person => person.MailingPostalAddressId);
			entity.Property(person => person.MailingAddressLine1).HasMaxLength(200);
			entity.Property(person => person.MailingAddressLine2).HasMaxLength(200);
			entity.Property(person => person.MailingCity).HasMaxLength(100);
			entity.Property(person => person.MailingState).HasMaxLength(100);
			entity.Property(person => person.MailingAddress).HasMaxLength(500).IsRequired();
			entity.Property(person => person.MailingPostalCode).HasMaxLength(20);
			entity.Property(person => person.PhoneNumber).HasMaxLength(50).IsRequired();
			entity.Property(person => person.BestTimeToContact).HasMaxLength(100);
			entity.Property(person => person.PreferredContactMethod).HasMaxLength(50);
			entity.Property(person => person.Email).HasMaxLength(256).IsRequired();
			entity.Property(person => person.IsArchived).HasDefaultValue(false);
			entity.HasIndex(person => new { person.TenantId, person.Email });
			entity.HasIndex(person => new { person.TenantId, person.IsArchived });
			entity.HasOne(person => person.PostalAddress)
				.WithMany(address => address.People)
				.HasForeignKey(person => person.PostalAddressId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(person => person.MailingPostalAddress)
				.WithMany(address => address.PersonMailingAddresses)
				.HasForeignKey(person => person.MailingPostalAddressId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<PersonPhone>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(phone => phone.Label).HasMaxLength(50).IsRequired();
			entity.Property(phone => phone.PhoneNumber).HasMaxLength(50).IsRequired();
			entity.HasOne(phone => phone.Person)
				.WithMany(person => person.Phones)
				.HasForeignKey(phone => phone.PersonId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<PersonEmail>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(email => email.Label).HasMaxLength(50).IsRequired();
			entity.Property(email => email.EmailAddress).HasMaxLength(256).IsRequired();
			entity.HasOne(email => email.Person)
				.WithMany(person => person.Emails)
				.HasForeignKey(email => email.PersonId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<Location>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(location => location.Nickname).HasMaxLength(200).IsRequired();
			entity.Property(location => location.PostalAddressId);
			entity.Property(location => location.AddressLine1).HasMaxLength(200);
			entity.Property(location => location.AddressLine2).HasMaxLength(200);
			entity.Property(location => location.City).HasMaxLength(100);
			entity.Property(location => location.State).HasMaxLength(100);
			entity.Property(location => location.HomeAddress).HasMaxLength(500).IsRequired();
			entity.Property(location => location.PostalCode).HasMaxLength(20);
			entity.Property(location => location.MailingPostalAddressId);
			entity.Property(location => location.MailingAddressLine1).HasMaxLength(200);
			entity.Property(location => location.MailingAddressLine2).HasMaxLength(200);
			entity.Property(location => location.MailingCity).HasMaxLength(100);
			entity.Property(location => location.MailingState).HasMaxLength(100);
			entity.Property(location => location.MailingAddress).HasMaxLength(500).IsRequired();
			entity.Property(location => location.MailingPostalCode).HasMaxLength(20);
			entity.Property(location => location.PhoneNumber).HasMaxLength(50).IsRequired();
			entity.Property(location => location.Email).HasMaxLength(256).IsRequired();
			entity.HasIndex(location => new { location.TenantId, location.PersonId });
			entity.HasOne(location => location.Person)
				.WithMany(person => person.Locations)
				.HasForeignKey(location => location.PersonId)
				.OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(location => location.PostalAddress)
				.WithMany(address => address.Locations)
				.HasForeignKey(location => location.PostalAddressId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(location => location.MailingPostalAddress)
				.WithMany(address => address.LocationMailingAddresses)
				.HasForeignKey(location => location.MailingPostalAddressId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<LocationPhone>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(phone => phone.Label).HasMaxLength(50).IsRequired();
			entity.Property(phone => phone.PhoneNumber).HasMaxLength(50).IsRequired();
			entity.HasOne(phone => phone.Location)
				.WithMany(location => location.Phones)
				.HasForeignKey(phone => phone.LocationId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<LocationEmail>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(email => email.Label).HasMaxLength(50).IsRequired();
			entity.Property(email => email.EmailAddress).HasMaxLength(256).IsRequired();
			entity.HasOne(email => email.Location)
				.WithMany(location => location.Emails)
				.HasForeignKey(email => email.LocationId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<Area>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(area => area.Name).HasMaxLength(200).IsRequired();
			entity.Property(area => area.Description).HasMaxLength(2000);
			entity.HasIndex(area => new { area.TenantId, area.Name });
		});

		builder.Entity<AreaCounty>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(county => county.CountyFips).HasMaxLength(5).IsRequired();
			entity.Property(county => county.CountyName).HasMaxLength(200).IsRequired();
			entity.Property(county => county.StateCode).HasMaxLength(2).IsRequired();
			entity.HasIndex(county => new { county.TenantId, county.AreaId, county.CountyFips }).IsUnique();
			entity.HasOne(county => county.Area)
				.WithMany(area => area.Counties)
				.HasForeignKey(county => county.AreaId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<SurveyDefinition>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(definition => definition.Name).HasMaxLength(200).IsRequired();
			entity.Property(definition => definition.Description).HasMaxLength(2000);
			entity.Property(definition => definition.IsArchived).HasDefaultValue(false);
			entity.HasIndex(definition => new { definition.TenantId, definition.IsArchived });
		});

		builder.Entity<Goal>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(goal => goal.Name).HasMaxLength(200).IsRequired();
			entity.Property(goal => goal.Description).HasMaxLength(2000);
			entity.HasIndex(goal => new { goal.TenantId, goal.AreaId });
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

		builder.Entity<SeedState>(entity =>
		{
			entity.HasKey(state => state.Key);
			entity.Property(state => state.Key).HasMaxLength(200).IsRequired();
		});

		builder.Entity<SurveyVersion>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(version => version.DisplayName).HasMaxLength(200).IsRequired();
			entity.Property(version => version.IsArchived).HasDefaultValue(false);
			entity.HasIndex(version => new { version.TenantId, version.SurveyDefinitionId, version.VersionNumber }).IsUnique();
			entity.HasIndex(version => new { version.TenantId, version.IsArchived });
			entity.HasOne(version => version.SurveyDefinition)
				.WithMany(definition => definition.Versions)
				.HasForeignKey(version => version.SurveyDefinitionId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<SurveySection>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(section => section.Title).HasMaxLength(200).IsRequired();
			entity.Property(section => section.Description).HasMaxLength(1000);
			entity.HasOne(section => section.SurveyVersion)
				.WithMany(version => version.Sections)
				.HasForeignKey(section => section.SurveyVersionId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<SurveyQuestion>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(question => question.Prompt).HasMaxLength(2000).IsRequired();
			entity.Property(question => question.HelpText).HasMaxLength(1000);
			entity.HasOne(question => question.SurveySection)
				.WithMany(section => section.Questions)
				.HasForeignKey(question => question.SurveySectionId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<QuestionOption>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(option => option.Label).HasMaxLength(200).IsRequired();
			entity.HasOne(option => option.SurveyQuestion)
				.WithMany(question => question.Options)
				.HasForeignKey(option => option.SurveyQuestionId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<SurveyAssignment>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
			entity.Property(assignment => assignment.PublicToken).HasMaxLength(100).IsRequired();
			entity.Property(assignment => assignment.CreatedByUserId).HasMaxLength(450);
			entity.Property(assignment => assignment.IsArchived).HasDefaultValue(false);
			entity.HasIndex(assignment => assignment.PublicToken).IsUnique();
			entity.HasIndex(assignment => new { assignment.TenantId, assignment.IsArchived });
			entity.HasIndex(assignment => new { assignment.TenantId, assignment.CreatedUtc });
			entity.HasOne(assignment => assignment.Location)
				.WithMany(location => location.Assignments)
				.HasForeignKey(assignment => assignment.LocationId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(assignment => assignment.LocationPhone)
				.WithMany(phone => phone.Assignments)
				.HasForeignKey(assignment => assignment.LocationPhoneId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(assignment => assignment.LocationEmail)
				.WithMany(email => email.Assignments)
				.HasForeignKey(assignment => assignment.LocationEmailId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(assignment => assignment.SurveyVersion)
				.WithMany(version => version.Assignments)
				.HasForeignKey(assignment => assignment.SurveyVersionId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<SurveyResponse>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
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
			entity.Property(response => response.RespondentMailingPostalAddressId);
			entity.Property(response => response.RespondentMailingAddressLine1).HasMaxLength(200);
			entity.Property(response => response.RespondentMailingAddressLine2).HasMaxLength(200);
			entity.Property(response => response.RespondentMailingCity).HasMaxLength(100);
			entity.Property(response => response.RespondentMailingState).HasMaxLength(100);
			entity.Property(response => response.RespondentMailingAddress).HasMaxLength(500).IsRequired();
			entity.Property(response => response.RespondentMailingPostalCode).HasMaxLength(20);
			entity.Property(response => response.RespondentCountyFipsSnapshot).HasMaxLength(5);
			entity.Property(response => response.RespondentCountyNameSnapshot).HasMaxLength(200);
			entity.Property(response => response.RespondentStateCodeSnapshot).HasMaxLength(2);
			entity.Property(response => response.RespondentPhoneNumber).HasMaxLength(50);
			entity.Property(response => response.RespondentPhoneLabel).HasMaxLength(50);
			entity.Property(response => response.RespondentBestTimeToContact).HasMaxLength(100);
			entity.Property(response => response.RespondentPreferredContactMethod).HasMaxLength(50);
			entity.Property(response => response.RespondentEmail).HasMaxLength(256);
			entity.Property(response => response.RespondentEmailLabel).HasMaxLength(50);
			entity.Property(response => response.SurveyNameSnapshot).HasMaxLength(200).IsRequired();
			entity.Property(response => response.SurveyVersionNameSnapshot).HasMaxLength(200).IsRequired();
			entity.HasIndex(response => response.SurveyAssignmentId).IsUnique();
			entity.HasIndex(response => new { response.TenantId, response.SubmittedUtc });
			entity.HasOne(response => response.SurveyAssignment)
				.WithOne(assignment => assignment.Response)
				.HasForeignKey<SurveyResponse>(response => response.SurveyAssignmentId)
				.OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(response => response.RespondentPostalAddress)
				.WithMany(address => address.SurveyResponses)
				.HasForeignKey(response => response.RespondentPostalAddressId)
				.OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(response => response.RespondentMailingPostalAddress)
				.WithMany(address => address.SurveyResponseMailingAddresses)
				.HasForeignKey(response => response.RespondentMailingPostalAddressId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<SurveyAnswer>(entity =>
		{
			ConfigureTenantOwnedEntity(entity);
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

	public override int SaveChanges(bool acceptAllChangesOnSuccess)
	{
		ApplyTenantOwnershipRules();
		return base.SaveChanges(acceptAllChangesOnSuccess);
	}

	public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
	{
		ApplyTenantOwnershipRules();
		return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
	}

	private void ConfigureTenantOwnedEntity<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
		where TEntity : class, ITenantOwned
	{
		entity.Property(item => item.TenantId).IsRequired();
		entity.HasIndex(nameof(ITenantOwned.TenantId));
		entity.HasQueryFilter(item => BypassTenantIsolation || (HasTenantContext && item.TenantId == CurrentTenantId));
	}

	private void ApplyTenantOwnershipRules()
	{
		foreach (var entry in ChangeTracker.Entries()
			.Where(item => item.Entity is ITenantOwned && item.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
		{
			if (BypassTenantIsolation)
			{
				continue;
			}

			if (!HasTenantContext)
			{
				throw new InvalidOperationException("A tenant context is required before saving tenant-owned data.");
			}

			var tenantProperty = entry.Property(nameof(ITenantOwned.TenantId));
			var currentTenantId = CurrentTenantId;
			var tenantId = Convert.ToInt32(tenantProperty.CurrentValue ?? tenantProperty.OriginalValue ?? 0);

			if (entry.State == EntityState.Added && tenantId == 0)
			{
				tenantProperty.CurrentValue = currentTenantId;
				continue;
			}

			if (tenantId != currentTenantId)
			{
				throw new InvalidOperationException("Cross-tenant writes are not allowed.");
			}

			tenantProperty.CurrentValue = currentTenantId;
		}
	}
}
