using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests;
using Testcontainers.Azurite;
using Testcontainers.PostgreSql;
[assembly: AssemblyFixture(typeof(DanceDbFixture))]

namespace TB.DanceDance.Tests;

public static class DockerHelper
{
    private const string AzuriteImage = "mcr.microsoft.com/azure-storage/azurite:latest";
    
    private static readonly AzuriteContainer AzuriteContainer = new AzuriteBuilder()
        .WithImage(AzuriteImage)
        .Build();
    
    public static async Task<AzuriteContainer> GetInitializedAzuriteContainer()
    {
        await AzuriteContainer.StartAsync();
        return AzuriteContainer;
    }
}
public class DanceDbFixture() : IAsyncLifetime
{
    private const string PostgresImage = "postgres";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithImage(PostgresImage)
        .Build();

    public DanceDbContext DbContextFactory()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DanceDbContext>()
            .UseNpgsql(container.GetConnectionString());
        
        DanceDbContext danceDbContext = new DanceDbContext(optionsBuilder.Options);
        return danceDbContext;
    }
    
    public ValueTask DisposeAsync()
    {
        return container.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync();
        await DbContextFactory().Database.EnsureCreatedAsync();
    }
}