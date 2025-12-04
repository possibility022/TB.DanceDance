using Microsoft.EntityFrameworkCore;

namespace TB.DanceDance.Tests.MigrationTests;

public abstract class BaseMigrationTests
{
    protected abstract DbContext CreateContext();

    protected async Task PerformUpMigrationTests(CancellationToken ct)
    {
        await using var db = CreateContext();
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync(ct);
        Assert.NotEmpty(pendingMigrations);
        
        await db.Database.MigrateAsync(ct);
    }
    
    protected async Task PerformDownMigrationTests(string firstMigration, CancellationToken ct)
    {
        await using var db = CreateContext();
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync(ct);
        Assert.Empty(pendingMigrations);
        await db.Database.MigrateAsync(firstMigration, ct);
    }
}