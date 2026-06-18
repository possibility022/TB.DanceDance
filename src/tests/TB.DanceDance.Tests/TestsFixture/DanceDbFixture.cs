using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;
using Testcontainers.PostgreSql;
[assembly: AssemblyFixture(typeof(DanceDbFixture))]

namespace TB.DanceDance.Tests.TestsFixture;

public class DanceDbFixture() : IAsyncLifetime
{
    private const string PostgresImage = "postgres";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder(PostgresImage)
        .Build();
    
    public DanceDbContext DbContextFactory(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DanceDbContext>()
            .UseNpgsql(connectionString);
        
        DanceDbContext danceDbContext = new DanceDbContext(optionsBuilder.Options);
        return danceDbContext;
    }
    
    public string DefaultConnectionString => container.GetConnectionString();
    
    public ValueTask DisposeAsync()
    {
        return container.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();
    }
    
    public string GetConnectionStringForThisClassSet(ITestContext testContext)
    {
        var name = testContext.TestClass?.TestClassSimpleName;
        var connectionString = container.GetConnectionString();
        
        if (name is null)
            return connectionString;

        return connectionString.Replace("Database=postgres", $"Database={name}");
    }
}