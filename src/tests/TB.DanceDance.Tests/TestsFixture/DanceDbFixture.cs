using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TB.DanceDance.Access;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Tests.TestsFixture;
using TB.DanceDance.Utilities.Infrastructure;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos;
using TB.DanceDance.Videos.Infrastructure;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(DanceDbFixture))]

namespace TB.DanceDance.Tests.TestsFixture;

/// <summary>
/// Shared fixture for the modular backend: one physical PostgreSQL container backing both
/// <see cref="AccessDbContext"/> (schema <c>access</c>) and <see cref="VideosDbContext"/>
/// (schemas <c>video</c>/<c>comments</c>/<c>sharing</c>). The schemas are disjoint and carry no
/// cross-module FKs, so each context is migrated independently against the same database.
/// </summary>
public class DanceDbFixture : IAsyncLifetime
{
    private const string PostgresImage = "postgres";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder(PostgresImage)
        .Build();

    public bool InitializeDbAtStart { get; set; } = true;

    public string ConnectionString => container.GetConnectionString();

    public AccessDbContext CreateAccessDbContext() =>
        new(new DbContextOptionsBuilder<AccessDbContext>().UseNpgsql(ConnectionString).Options);

    public VideosDbContext CreateVideosDbContext() =>
        new(new DbContextOptionsBuilder<VideosDbContext>().UseNpgsql(ConnectionString).Options);

    /// <summary>
    /// Builds a service provider wired exactly like the API: both module DbContexts against the test
    /// database, the blob factory, and every Access/Videos mediator handler. Resolve
    /// <c>IRequestHandler&lt;TRequest, TResponse&gt;</c> from it to drive the real (cross-module) path.
    /// </summary>
    public ServiceProvider BuildServiceProvider(string blobConnectionString)
    {
        var services = new ServiceCollection();

        services.AddAccessModuleInfrastructure(ConnectionString);
        services.AddVideosModuleInfrastructure(ConnectionString);
        services.AddSingleton<IBlobDataServiceFactory>(new BlobDataServiceFactory(blobConnectionString));

        services
            .AddMediator()
            .AddAccessModule()
            .AddVideosModule();

        return services.BuildServiceProvider();
    }

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();
        if (InitializeDbAtStart)
        {
            await CreateAccessDbContext().Database.MigrateAsync();
            await CreateVideosDbContext().Database.MigrateAsync();
        }
    }

    public ValueTask DisposeAsync()
    {
        return container.DisposeAsync();
    }
}
