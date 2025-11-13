using IdentityServer4.EntityFramework.DbContexts;
using Infrastructure.Identity;
using Testcontainers.PostgreSql;

namespace TB.DanceDance.Tests.TestsFixture;

public class PersistedGrantDbFixture : IAsyncLifetime
{
    private const string PostgresImage = "postgres";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithImage(PostgresImage)
        .Build();
    
    public PersistedGrantDbContext DbContextFactory()
    {
        return DesignTimeContextFactory.CreatePersistedGrantDbContext(container.GetConnectionString());
    }
    
    public ValueTask DisposeAsync()
    {
        return container.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();
    }
}