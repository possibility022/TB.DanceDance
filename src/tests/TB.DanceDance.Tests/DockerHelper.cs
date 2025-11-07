using DotNet.Testcontainers.Builders;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests;
using Testcontainers.Azurite;
using Testcontainers.PostgreSql;
[assembly: AssemblyFixture(typeof(DanceDbFixture))]
[assembly: AssemblyFixture(typeof(BlobStorageFixture))]

namespace TB.DanceDance.Tests;

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

public class BlobStorageFixture() : IAsyncLifetime
{
    private const string AzuriteImage = "mcr.microsoft.com/azure-storage/azurite";
    
    private readonly AzuriteContainer container = new AzuriteBuilder()
        .WithImage(AzuriteImage)
        .Build();


    public string GetConnectionString()
    {
        return container.GetConnectionString();
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