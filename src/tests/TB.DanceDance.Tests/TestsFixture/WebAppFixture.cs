extern alias ApiHost;

using System.Security.Claims;
using FastEndpoints.Testing;
using Infrastructure.Data;
using Infrastructure.Data.BlobStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(TB.DanceDance.Tests.TestsFixture.WebAppFixture))]

namespace TB.DanceDance.Tests.TestsFixture;

/// <summary>
/// Boots the real API host (FastEndpoints + auth policies) against throwaway Postgres/Azurite
/// containers, so tests can exercise endpoint-level behavior (e.g. scope-based 403s) that no
/// service-level test can observe. The real JWT bearer scheme is replaced with one that trusts
/// the <see cref="SubHeader"/>/<see cref="ScopeHeader"/> request headers, so tests can simulate any
/// user/scope without a real OpenIddict token.
/// </summary>
public class WebAppFixture : AppFixture<ApiHost::Program>
{
    public const string SubHeader = "X-Test-Sub";
    public const string ScopeHeader = "X-Test-Scope";

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres").Build();
    private readonly AzuriteContainer azurite = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite").Build();

    protected override async ValueTask PreSetupAsync()
    {
        await Task.WhenAll(postgres.StartAsync(), azurite.StartAsync());
    }

    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:PostgreDb", postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Blob", azurite.GetConnectionString());
        builder.UseSetting("Authentication:Authority", "https://test-authority.invalid/");
        builder.UseSetting("Authentication:Audience", "tbdancedanceapi");
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // Bypass real OIDC token validation: short-circuit the JWT bearer handler with claims
        // derived from test-only headers, so the existing scope policies (registered against the
        // real "Bearer" scheme in Program.cs) can be exercised without a real auth server.
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var sub = context.Request.Headers[SubHeader].FirstOrDefault();
                    if (string.IsNullOrEmpty(sub))
                        return Task.CompletedTask;

                    var scopes = context.Request.Headers[ScopeHeader].FirstOrDefault() ?? string.Empty;
                    var identity = new ClaimsIdentity(JwtBearerDefaults.AuthenticationScheme);
                    identity.AddClaim(new Claim("sub", sub));
                    identity.AddClaim(new Claim("scope", scopes));
                    context.Principal = new ClaimsPrincipal(identity);
                    context.Success();
                    return Task.CompletedTask;
                }
            };
        });
    }

    protected override async ValueTask SetupAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DanceDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    protected override async ValueTask TearDownAsync()
    {
        await postgres.DisposeAsync();
        await azurite.DisposeAsync();
    }

    public DanceDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DanceDbContext>().UseNpgsql(postgres.GetConnectionString());
        return new DanceDbContext(optionsBuilder.Options);
    }

    public BlobDataServiceFactory CreateBlobFactory() => new(azurite.GetConnectionString());

    public HttpClient CreateAuthorizedClient(string userId, string scope)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(SubHeader, userId);
        client.DefaultRequestHeaders.Add(ScopeHeader, scope);
        return client;
    }

    /// <summary>A client with no auth headers at all, for exercising signed-out/anonymous behavior.</summary>
    public HttpClient CreateAnonymousClient() => CreateClient();
}
