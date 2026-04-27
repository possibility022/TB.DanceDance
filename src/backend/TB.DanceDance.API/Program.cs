using Application;
using Domain.Exceptions;
using IdentityServer4;
using Infrastructure;
using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using TB.DanceDance.API;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true);
}

if (builder.Environment.IsProduction())
{
    builder.Configuration.AddJsonFile("appsettings.Production.json", optional: true);
}

if (builder.Environment.IsEnvironment("QA"))
{
    builder.Configuration.AddJsonFile("appsettings.QA.json", optional: true);
}

OtelConfiguration.ConfigureOpenTelemetryAndLogging(builder);

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.Position));

builder.Services.RegisterApplicationServices();
builder.Services.RegisterInfrastructureServices(builder.Configuration, builder.Environment.IsProduction());

builder.Services.AddControllersWithViews();
builder.Services.AddCors(setup =>
{
    setup.AddDefaultPolicy(c =>
    {
        if (builder.Environment.IsDevelopment())
        {
            c.WithOrigins(CorsConfigProvider.GetDevOrigins());
        }
        else
        {
            var config = CorsConfigProvider.GetFromConfiguration(builder.Configuration);
            c.WithOrigins(config);
        }

        c.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowedToAllowWildcardSubdomains();

    });
});

var authority = builder.Configuration["Authentication:Authority"]
                ?? throw new AppException("'Authentication:Authority' is not configured.");
var audience = builder.Configuration["Authentication:Audience"]
               ?? throw new AppException("'Authentication:Audience' is not configured.");
var requireHttpsMetadata = builder.Configuration.GetValue("Authentication:RequireHttpsMetadata", true);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = audience;
        options.RequireHttpsMetadata = requireHttpsMetadata;
        options.MapInboundClaims = false;
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(DanceDanceResources.WestCoastSwing.Scopes.ReadScope, policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => HasScope(context.User, DanceDanceResources.WestCoastSwing.Scopes.ReadScope));
    });

    o.AddPolicy(DanceDanceResources.WestCoastSwing.Scopes.WriteConvert, policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => HasScope(context.User, DanceDanceResources.WestCoastSwing.Scopes.WriteConvert));
    });
});

/*
 * Previous (IdentityServer-in-API) setup kept intentionally for migration comparison:
 *
 * builder.Services.AddAuthorization(o =>
 * {
 *     o.AddPolicy(DanceDanceResources.WestCoastSwing.Scopes.ReadScope, c =>
 *     {
 *         c.AddAuthenticationSchemes(IdentityServerConstants.LocalApi.AuthenticationScheme);
 *         c.RequireAuthenticatedUser();
 *     });
 *
 *     o.AddPolicy(DanceDanceResources.WestCoastSwing.Scopes.WriteConvert, c =>
 *     {
 *         c.RequireClaim(DanceDanceResources.WestCoastSwing.Scopes.WriteConvert);
 *         c.RequireAuthenticatedUser();
 *     });
 * });
 *
 * var authBuilder = builder.Services
 *     .AddAuthentication()
 *     .AddLocalApi(o =>
 *     {
 *         o.ExpectedScope = DanceDanceResources.WestCoastSwing.Scopes.ReadScope;
 *     });
 *
 * var configureGoogleAuth = builder.Configuration["Authentication:Google:ClientId"] != null;
 *
 * if (configureGoogleAuth)
 * {
 *     authBuilder.AddGoogle(googleOptions =>
 *      {
 *          googleOptions.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
 *
 *          googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new AppException("Google Client Id is null.");
 *          googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? throw new AppException("Google Client Secret is null.");
 *      });
 * }
 */

builder.Services.AddScoped<IIdentityClient, IdentityClient>();

var app = builder.Build();
app.UseCors();
#if DEBUG
// Enable http for debug
#else

var noHttps = Environment.GetEnvironmentVariable("TB.DanceDance.NoHttps");
Console.WriteLine("NoHttps: {0}", noHttps);
if (string.IsNullOrEmpty(noHttps) || !noHttps.Equals("true", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}
#endif


app.UseStaticFiles();
app.UseAuthentication();
// app.UseIdentityServer(); // Disabled during migration to dedicated auth server (TB.Auth.Web).
app.UseAuthorization();
app.MapControllers();

app.Run();

static bool HasScope(ClaimsPrincipal user, string expectedScope)
{
    return user.Claims
        .Where(claim => claim.Type is "scope" or "scp")
        .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Any(scope => string.Equals(scope, expectedScope, StringComparison.Ordinal));
}


