using Infrastructure.Identity;
using Testcontainers.PostgreSql;

namespace TB.DanceDance.Tests.TestsFixture;

public class IdentityStoreFixture : IAsyncLifetime
{
    private const string PostgresImage = "postgres";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithImage(PostgresImage)
        .Build();

    
    public IdentityStoreContext DbContextFactory()
    {
        return DesignTimeContextFactory.CreateIdentityStoreContext(container.GetConnectionString());
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