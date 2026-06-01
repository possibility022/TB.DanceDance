using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.MigrationTests;

/// <summary>
/// The old single DanceDbContext is now two module contexts (AccessDbContext + VideosDbContext), each
/// owning disjoint schemas of the same physical database. Each ships a single initial migration; we
/// verify both up (apply) and down (revert to "0").
/// </summary>
[TestCaseOrderer(typeof(PriorityOrderer))]
public class AccessDbMigrationTests : IAsyncLifetime
{
    private readonly DanceDbFixture dbFixture = new();

    public async ValueTask InitializeAsync()
    {
        dbFixture.InitializeDbAtStart = false;
        await dbFixture.InitializeAsync();
    }

    public ValueTask DisposeAsync() => dbFixture.DisposeAsync();

    [Fact, TestPriority(1)]
    public async Task UpMigrationsCanRun()
    {
        await dbFixture.CreateAccessDbContext().Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    [Fact, TestPriority(2)]
    public async Task DownMigrationsCanRun()
    {
        await dbFixture.CreateAccessDbContext().Database.MigrateAsync(TestContext.Current.CancellationToken);
        await dbFixture.CreateAccessDbContext().Database.MigrateAsync("0", TestContext.Current.CancellationToken);
    }
}

[TestCaseOrderer(typeof(PriorityOrderer))]
public class VideosDbMigrationTests : IAsyncLifetime
{
    private readonly DanceDbFixture dbFixture = new();

    public async ValueTask InitializeAsync()
    {
        dbFixture.InitializeDbAtStart = false;
        await dbFixture.InitializeAsync();
    }

    public ValueTask DisposeAsync() => dbFixture.DisposeAsync();

    [Fact, TestPriority(1)]
    public async Task UpMigrationsCanRun()
    {
        await dbFixture.CreateVideosDbContext().Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    [Fact, TestPriority(2)]
    public async Task DownMigrationsCanRun()
    {
        await dbFixture.CreateVideosDbContext().Database.MigrateAsync(TestContext.Current.CancellationToken);
        await dbFixture.CreateVideosDbContext().Database.MigrateAsync("0", TestContext.Current.CancellationToken);
    }
}
