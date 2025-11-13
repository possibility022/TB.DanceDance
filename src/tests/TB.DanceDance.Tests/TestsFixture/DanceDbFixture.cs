using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;
using Testcontainers.PostgreSql;
[assembly: AssemblyFixture(typeof(DanceDbFixture))]

namespace TB.DanceDance.Tests.TestsFixture;

public class DanceDbFixture() : IAsyncLifetime
{
    private const string PostgresImage = "postgres";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithImage(PostgresImage)
        .Build();

    public bool InitializeDbAtStart { get; set; } = true;
    
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
        if (InitializeDbAtStart)
            await DbContextFactory().Database.EnsureCreatedAsync();
    }
}