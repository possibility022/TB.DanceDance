using Application;
using Domain.Exceptions;
using Infrastructure;
using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.RegisterInfrastructureServices(builder.Configuration);

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

// Optional override for the OIDC discovery fetch URL, independent of Authority.
//
// Problem: JWT Bearer uses Authority for two distinct purposes:
//   1. Building the discovery URL: {Authority}/.well-known/openid-configuration
//   2. Validating the 'iss' claim in JWT tokens (via the issuer returned in the discovery doc)
//
// In Docker these two URLs cannot be the same value:
//   - The auth server's public issuer is https://localhost:7259  (matches tokens, reachable by browsers)
//   - Discovery must be fetched via http://host.docker.internal:5296 (reachable from inside the API container)
//
// Setting MetadataAddress decouples the fetch URL (#1) from the issuer validation (#2),
// so Authority can stay equal to the real public issuer while discovery is fetched from a
// container-internal HTTP endpoint. Leave this unset outside of Docker.
var metadataAddress = builder.Configuration["Authentication:MetadataAddress"];
var validIssuers = builder.Configuration["Authentication:ValidIssuers"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = audience;

        if (string.IsNullOrWhiteSpace(validIssuers) is false)
        {
            // IDX10204: Unable to validate issuer on K8s if not set
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidIssuers = validIssuers?.Split(';',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                // IDX10500: Signature validation failed. No security keys were provided to validate the signature on K8s
                SignatureValidator = delegate(string token, TokenValidationParameters parameters)
                {
                    var jwt = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token);
                    return jwt;
                }
            };
        }

        options.RequireHttpsMetadata = requireHttpsMetadata;
        options.MapInboundClaims = false;
        if (!string.IsNullOrWhiteSpace(metadataAddress))
            options.MetadataAddress = metadataAddress;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (!string.IsNullOrWhiteSpace(context.Token))
                    return Task.CompletedTask;

                if (context.Request.Headers.ContainsKey("Authorization"))
                    return Task.CompletedTask;

                var path = context.Request.Path.Value;
                if (string.IsNullOrWhiteSpace(path) ||
                    !path.StartsWith("/api/videos/", StringComparison.OrdinalIgnoreCase) ||
                    !path.EndsWith("/stream", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.CompletedTask;
                }

                var rawToken = context.Request.Query["token"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(rawToken))
                    return Task.CompletedTask;

                const string bearerPrefix = "Bearer ";
                context.Token = rawToken.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
                    ? rawToken[bearerPrefix.Length..].Trim()
                    : rawToken.Trim();

                return Task.CompletedTask;
            }
        };
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

