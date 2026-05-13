using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.MigrationTests;

[TestCaseOrderer(typeof(PriorityOrderer))]
public class AuthStoreMigrationTests: BaseMigrationTests, IClassFixture<AuthStoreFixture>
{
    private readonly AuthStoreFixture dbFixture;

    public AuthStoreMigrationTests(AuthStoreFixture dbFixture)
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
        await PerformDownMigrationTests("20260430212148_AuthInit", TestContext.Current.CancellationToken);
    }

    protected override DbContext CreateContext() => dbFixture.DbContextFactory();
}