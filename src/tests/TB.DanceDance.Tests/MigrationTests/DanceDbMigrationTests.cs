using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.MigrationTests;

[TestCaseOrderer(typeof(PriorityOrderer))]
public class DanceDbMigrationTests : IAsyncLifetime
{
    private readonly DanceDbFixture dbFixture = new();
    private readonly string connectionString;

    public DanceDbMigrationTests()
    {
        connectionString = dbFixture.GetConnectionStringForThisClassSet(TestContext.Current);
    }
    
    public async ValueTask DisposeAsync()
    {
        await dbFixture.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await dbFixture.InitializeAsync();
    }

    [Fact, TestPriority(2)]
    public async Task UpMigrationsCanRun()
    {
        await dbFixture.DbContextFactory(connectionString).Database.MigrateAsync(TestContext.Current.CancellationToken);
    }
    
    [Fact, TestPriority(1)]
    public async Task DownMigrationsCanRun()
    {
        await dbFixture.DbContextFactory(connectionString).Database.MigrateAsync("20230617222723_Initial", TestContext.Current.CancellationToken);
    }
}