using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Survey.Domain;
using Survey.Infrastructure;
using Survey.Infrastructure.Identity;
using Survey.ServiceDefaults;
using Survey.Web.Components;
using Survey.Web.Components.Account;
using Survey.Web.Components.Shared;
using Survey.Web.Importing;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<ToastService>();

builder.Services.AddAuthentication(options =>
	{
		options.DefaultScheme = IdentityConstants.ApplicationScheme;
		options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
	})
	.AddIdentityCookies();

builder.Services.AddAuthorizationBuilder();
builder.Services.AddSurveyInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

await app.Services.InitializeSurveyPlatformAsync();

if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();
}
else
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapDefaultEndpoints();
app.MapGet("/admin/import-samples/{entity}", (string entity) =>
	{
		if (!ImportSampleWorkbookBuilder.TryGetDefinition(entity, out var definition))
		{
			return Results.NotFound();
		}

		var bytes = ImportSampleWorkbookBuilder.BuildWorkbook(definition);
		return Results.File(bytes, ImportSampleWorkbookBuilder.ContentType, definition.FileName);
	})
	.RequireAuthorization(new AuthorizeAttribute
	{
		Roles = RoleNames.Admin
	});
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();
app.MapAdditionalIdentityEndpoints();

app.Run();

public partial class Program
{
}
