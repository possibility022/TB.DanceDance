using Infrastructure.Data;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests;

public abstract class BaseTestClass : IAsyncLifetime
{
    private readonly DanceDbFixture dbContextFixture;
    protected readonly DanceDbContext SeedDbContext;
    private readonly DanceDbContext runtimeDbContext;
    private readonly string ConnectionString;
    
    protected BaseTestClass(DanceDbFixture dbContextFixture)
    {
        this.ConnectionString = dbContextFixture.GetConnectionStringForThisClassSet(TestContext.Current);
        this.dbContextFixture = dbContextFixture;
        SeedDbContext = dbContextFixture.DbContextFactory(ConnectionString);
        runtimeDbContext = dbContextFixture.DbContextFactory(ConnectionString);
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

    public async ValueTask InitializeAsync()
    {
        var dbContext = dbContextFixture.DbContextFactory(ConnectionString);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        
        await Initialize(runtimeDbContext);
    }
}