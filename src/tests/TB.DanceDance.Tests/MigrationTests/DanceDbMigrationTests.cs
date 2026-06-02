using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.MigrationTests;

[TestCaseOrderer(typeof(PriorityOrderer))]
public class DanceDbMigrationTests : IAsyncLifetime
{
    private readonly DanceDbFixture dbFixture = new DanceDbFixture();
    
    public async ValueTask DisposeAsync()
    {
        await dbFixture.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        dbFixture.InitializeDbAtStart = false;
        await dbFixture.InitializeAsync();
    }

    [Fact, TestPriority(2)]
    public async Task UpMigrationsCanRun()
    {
        await dbFixture.DbContextFactory().Database.MigrateAsync(TestContext.Current.CancellationToken);
    }
    
    [Fact, TestPriority(1)]
    public async Task DownMigrationsCanRun()
    {
        await dbFixture.DbContextFactory().Database.MigrateAsync("20230617222723_Initial", TestContext.Current.CancellationToken);
    }
}