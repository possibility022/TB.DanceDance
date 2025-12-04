using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.MigrationTests;

[TestCaseOrderer(typeof(PriorityOrderer))]
public class IdentityStoreMigrationTests: BaseMigrationTests, IClassFixture<IdentityStoreFixture>
{
    private readonly IdentityStoreFixture dbFixture;

    public IdentityStoreMigrationTests(IdentityStoreFixture dbFixture)
    {
        this.dbFixture = dbFixture;
    }

    [Fact, TestPriority(1)]
    public async Task UpMigrationsCanRun()
    {
        await PerformUpMigrationTests(TestContext.Current.CancellationToken);
    }
    
    [Fact, TestPriority(2)]
    public async Task DownMigrationsCanRun()
    {
        await PerformDownMigrationTests("20230617221050_Initial", TestContext.Current.CancellationToken);
    }

    protected override DbContext CreateContext() => dbFixture.DbContextFactory();
}