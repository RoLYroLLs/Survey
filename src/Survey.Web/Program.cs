using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using NLog.Web;
using Survey.Application.Services;
using Survey.Domain;
using Survey.Infrastructure;
using Survey.Infrastructure.Configuration;
using Survey.Infrastructure.Identity;
using Survey.Infrastructure.Security;
using Survey.ServiceDefaults;
using Survey.Web.Components;
using Survey.Web.Components.Account;
using Survey.Web.Components.Shared;
using Survey.Web.Importing;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

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
builder.Services.AddScoped<IEmailSender<ApplicationUser>, QueuedIdentityEmailSender>();

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
app.Use(async (context, next) =>
	{
		var setupStateService = context.RequestServices.GetRequiredService<Survey.Infrastructure.Persistence.InitialSetupStateService>();
		var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
		var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
		var requestPath = context.Request.Path;
		var path = requestPath.Value?.Trim('/') ?? string.Empty;
		var isAuthenticated = context.User.Identity?.IsAuthenticated == true;

		static bool IsStaticAssetPath(PathString currentPath)
		{
			if (currentPath.StartsWithSegments("/_blazor", StringComparison.OrdinalIgnoreCase)
				|| currentPath.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase)
				|| currentPath.StartsWithSegments("/_content", StringComparison.OrdinalIgnoreCase)
				|| currentPath.StartsWithSegments("/lib", StringComparison.OrdinalIgnoreCase)
				|| currentPath.StartsWithSegments("/js", StringComparison.OrdinalIgnoreCase)
				|| currentPath.StartsWithSegments("/css", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			var value = currentPath.Value;
			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}

			return Path.HasExtension(value);
		}

		static bool IsAllowedIncompletePath(string currentPath)
		{
			return string.Equals(currentPath, "setup", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(currentPath, "setup/platform-admin", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(currentPath, "setup/initial-seeding", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(currentPath, "Error", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(currentPath, "not-found", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(currentPath, "Account/Login", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(currentPath, "Account/Logout", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(currentPath, "Account/ClearStaleAuth", StringComparison.OrdinalIgnoreCase)
				|| currentPath.StartsWith("email/track/", StringComparison.OrdinalIgnoreCase);
		}

		if (IsStaticAssetPath(requestPath))
		{
			await next();
			return;
		}

		var setupState = await setupStateService.GetStatusAsync(context.RequestAborted);
		if (!setupState.HasUsers)
		{
			if (isAuthenticated
				&& !string.Equals(path, "Account/ClearStaleAuth", StringComparison.OrdinalIgnoreCase))
			{
				var returnUrl = Uri.EscapeDataString("/setup");
				context.Response.Redirect($"/Account/ClearStaleAuth?returnUrl={returnUrl}");
				return;
			}

			if (!string.Equals(path, "setup", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(path, "Account/ClearStaleAuth", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(path, "setup/platform-admin", StringComparison.OrdinalIgnoreCase))
			{
				context.Response.Redirect("/setup");
				return;
			}
		}
		else if (!setupState.IsComplete)
		{
			if (string.Equals(path, "setup/initial-seeding", StringComparison.OrdinalIgnoreCase))
			{
				if (!isAuthenticated)
				{
					context.Response.Redirect("/Account/Login");
					return;
				}

				var currentUser = await userManager.GetUserAsync(context.User);
				if (currentUser is null || !currentUser.IsBootstrapPlatformOwner)
				{
					await signInManager.SignOutAsync();
					context.Response.Redirect("/Account/Login");
					return;
				}

				await next();
				return;
			}

			if (!IsAllowedIncompletePath(path))
			{
				if (!isAuthenticated)
				{
					context.Response.Redirect("/Account/Login");
					return;
				}

				var currentUser = await userManager.GetUserAsync(context.User);
				if (currentUser is null || !currentUser.IsBootstrapPlatformOwner)
				{
					await signInManager.SignOutAsync();
					context.Response.Redirect("/Account/Login");
					return;
				}

				context.Response.Redirect("/setup/initial-seeding");
				return;
			}
		}

		await next();
	});
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapDefaultEndpoints();
var backgroundJobsOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<BackgroundJobsOptions>>().Value;
if (backgroundJobsOptions.Enabled)
{
	app.MapHangfireDashboard(backgroundJobsOptions.DashboardPath, new DashboardOptions
	{
		Authorization =
		[
			new HangfireDashboardAuthorizationFilter()
		]
	});
}
app.MapGet("/admin/import-samples/{entity}", (string entity) =>
	{
		if (!ImportSampleWorkbookBuilder.TryGetDefinition(entity, out var definition))
		{
			return Results.NotFound();
		}

		var bytes = ImportSampleWorkbookBuilder.BuildWorkbook(definition);
		return Results.File(bytes, ImportSampleWorkbookBuilder.ContentType, definition.FileName);
	})
	.RequireAuthorization(SurveyAuthorizationPolicies.PlatformPermission(PlatformPermissionKeys.GeographyView));
app.MapGet("/app/switch-tenant/{membershipId:int}", async (int membershipId, ITenantContextAccessor tenantContextAccessor) =>
	{
		await tenantContextAccessor.SwitchActiveTenantAsync(membershipId);

		return Results.LocalRedirect("/app");
	})
	.RequireAuthorization(SurveyAuthorizationPolicies.TenantAccess);
var transparentTrackingPixel = Convert.FromBase64String("R0lGODlhAQABAIABAP///wAAACwAAAAAAQABAAACAkQBADs=");
app.MapGet("/email/track/open/{token}", async (string token, HttpContext httpContext, IEmailTrackingService emailTrackingService) =>
	{
		var userAgent = httpContext.Request.Headers.UserAgent.ToString();
		var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
		await emailTrackingService.TrackOpenAsync(token, userAgent, ipAddress, httpContext.RequestAborted);
		return Results.File(transparentTrackingPixel, "image/gif");
	})
	.AllowAnonymous();
app.MapGet("/email/track/click/{token}", async (string token, string? url, string? kind, HttpContext httpContext, IEmailTrackingService emailTrackingService) =>
	{
		var userAgent = httpContext.Request.Headers.UserAgent.ToString();
		var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
		var result = await emailTrackingService.TrackClickAsync(token, kind, url, userAgent, ipAddress, httpContext.RequestAborted);
		if (!result.IsValid)
		{
			return Results.NotFound();
		}

		return result.DestinationUrl.StartsWith("/", StringComparison.Ordinal)
			? Results.LocalRedirect(result.DestinationUrl)
			: Results.Redirect(result.DestinationUrl);
	})
	.AllowAnonymous();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode()
	.AllowAnonymous();
app.MapAdditionalIdentityEndpoints();

app.Run();

public partial class Program
{
}
