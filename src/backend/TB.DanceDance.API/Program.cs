using Application;
using Application.Features.AccessManagement;
using Domain.Exceptions;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using TB.DanceDance.API;
using TB.DanceDance.API.Contracts.ApiResources;

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

builder.Services.Configure<Application.AppOptions>(builder.Configuration.GetSection(Application.AppOptions.Position));

builder.Services.RegisterApplicationServices();
builder.Services.RegisterInfrastructureServices(builder.Configuration);

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

// Optional: override the OIDC discovery URL independently of Authority.
// Useful in Docker where the auth server's public URL isn't reachable from inside the container.
var metadataAddress = builder.Configuration["Authentication:MetadataAddress"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = audience;
        options.RequireHttpsMetadata = requireHttpsMetadata;
        options.MapInboundClaims = false;
        if (!string.IsNullOrWhiteSpace(metadataAddress))
            options.MetadataAddress = metadataAddress;
        if (builder.Environment.IsDevelopment())
        {
            // Dev cert is self-signed and issued for localhost; bypass validation for OIDC backchannel only.
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }
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


app.UseAuthentication();
app.UseAuthorization();
app.UseApplicationEndpoints();
app.MapGet("policy/dancedanceapp", (IConfiguration config) =>
{
    var authority = config["Authentication:Authority"]?.TrimEnd('/') ?? string.Empty;
    return Results.Redirect($"{authority}/policy/dancedanceapp", permanent: true);
});

app.Run();

static bool HasScope(ClaimsPrincipal user, string expectedScope)
{
    return user.Claims
        .Where(claim => claim.Type is "scope" or "scp")
        .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Any(scope => string.Equals(scope, expectedScope, StringComparison.Ordinal));
}

