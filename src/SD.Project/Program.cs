using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SD.Project.Application;
using SD.Project.Application.Services;
using SD.Project.Infrastructure;
using SD.Project.Pages;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add antiforgery protection for CSRF
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    // Use SameAsRequest to support both HTTP (dev) and HTTPS (prod) scenarios
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Configure cookie authentication with OAuth providers
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/Error";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    // Use SameAsRequest to support both HTTP (dev) and HTTPS (prod) scenarios
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax; // Lax for OAuth redirects

    // Validate session token on each request
    options.Events = new CookieAuthenticationEvents
    {
        OnValidatePrincipal = async context =>
        {
            var sessionToken = context.Principal?.FindFirst(LoginModel.SessionTokenClaimType)?.Value;
            if (string.IsNullOrEmpty(sessionToken))
            {
                // No session token in cookie - reject the session for authenticated users
                if (context.Principal?.Identity?.IsAuthenticated == true)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
                return;
            }

            // Validate the session token against the database
            var sessionService = context.HttpContext.RequestServices.GetRequiredService<SessionService>();
            var session = await sessionService.ValidateSessionAsync(sessionToken);

            if (session is null)
            {
                // Session is invalid (expired or revoked) - reject and sign out
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            // Update session activity for activity tracking
            await sessionService.UpdateSessionActivityAsync(sessionToken);
        }
    };
});

// Configure Google OAuth if credentials are provided
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
    });
}

// Configure Facebook OAuth if credentials are provided
var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        options.CallbackPath = "/signin-facebook";
    });
}

builder.Services.AddRazorPages();

// Add distributed memory cache for session storage
builder.Services.AddDistributedMemoryCache();

// Add session support for filter persistence
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Configure HSTS for production environments
// These settings enforce HTTPS and protect against protocol downgrade attacks
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365); // 1 year is recommended for production
});

// Configure HTTPS redirection
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 443;
});

var app = builder.Build();

// Seed development data
if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDevelopmentDataAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // Configure HSTS for production with stricter settings
    // HSTS enforces HTTPS connections and protects against protocol downgrade attacks
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
