using Infrastructure.Data;
using TB.DanceDance.Tests.TestsFixture;

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

    protected virtual ValueTask BeforeDispose(DanceDbContext runtimeDbContext)
    {
        // nothing here
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await BeforeDispose(runtimeDbContext);
        await SeedDbContext.DisposeAsync();
    }

    public ValueTask InitializeAsync()
    {
        return Initialize(runtimeDbContext);
    }
}