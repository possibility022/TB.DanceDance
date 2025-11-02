using Infrastructure.Data;

namespace TB.DanceDance.Tests;

public class UnitTest1
{
    private readonly DanceDbContext dbContext;

    public UnitTest1(DanceDbFixture danceDbFixture)
    {
        this.dbContext = danceDbFixture.DbContextFactory();
    }
    
    [Fact]
    public void Test1()
    {
        var queryRes = dbContext.AssingedToEvents.ToList();
    }
}