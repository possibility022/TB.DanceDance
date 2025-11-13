using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.MigrationTests;

[TestCaseOrderer(typeof(PriorityOrderer))]
public class ConfigurationDbMigrationTests : BaseMigrationTests, IClassFixture<ConfigurationDbFixture>
{
    private readonly ConfigurationDbFixture dbFixture;

    public ConfigurationDbMigrationTests(ConfigurationDbFixture dbFixture)
    {
        this.dbFixture = dbFixture;
    }

    [Fact(), TestPriority(1)]
    public async Task UpMigrationsCanRun()
    {
        await PerformUpMigrationTests(TestContext.Current.CancellationToken);
    }
    
    [Fact(), TestPriority(2)]
    public async Task DownMigrationsCanRun()
    {
        await PerformDownMigrationTests("20230617221040_Initial", TestContext.Current.CancellationToken);
    }

    protected override DbContext CreateContext() => dbFixture.DbContextFactory();
}