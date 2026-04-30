using TB.Auth.Web.Identity;
using Testcontainers.PostgreSql;

namespace TB.DanceDance.Tests.TestsFixture;

public class AuthStoreFixture : IAsyncLifetime
{
    private const string PostgresImage = "postgres";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder(PostgresImage)
        .Build();

    
    public AuthStoreContext DbContextFactory()
    {
        return DesignTimeContextFactory.CreateAuthStoreContext(container.GetConnectionString());
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