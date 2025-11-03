using Infrastructure.Data;

namespace TB.DanceDance.Tests;

public abstract class BaseTestClass : IAsyncLifetime
{
    protected readonly DanceDbContext SeedDbContext;
    private readonly DanceDbContext runtimeDbContext;
    
    protected BaseTestClass(DanceDbFixture dbContextFixture)
    {
        SeedDbContext = dbContextFixture.DbContextFactory();
        runtimeDbContext = dbContextFixture.DbContextFactory();
    }

    protected abstract ValueTask Initialize(DanceDbContext runtimeDbContext);

    public ValueTask DisposeAsync()
    {
        return SeedDbContext.DisposeAsync();
    }

    public ValueTask InitializeAsync()
    {
        return Initialize(runtimeDbContext);
    }
}