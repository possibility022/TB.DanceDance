using Application.Services;
using Infrastructure.Data;

namespace TB.DanceDance.Tests.Application;

public class EventServiceTests : IDisposable, IAsyncDisposable
{
    private readonly EventService  eventService;
    private readonly DanceDbContext dbContext;
    private readonly DanceDbContext seedContext;

    public EventServiceTests(DanceDbFixture danceDbFixture)
    {
        this.seedContext = danceDbFixture.DbContextFactory();
        this.dbContext = danceDbFixture.DbContextFactory();
        this.eventService = new EventService(dbContext);
    }

    [Fact]
    public async Task EventService_ReturnsVideosAssignedToEvents_ThatUserHasAccessTo()
    {
        var testData = TestDataFactory.OneUserAssignedToEvent_WithOneVideo();
        seedContext.Add(testData.owner);
        seedContext.Add(testData.user);
        seedContext.Add(testData.evt);
        seedContext.Add(testData.video);
        seedContext.Add(testData.eventShare);
        seedContext.Add(testData.participation);

        await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        var videos = eventService.GetVideos(testData.evt.Id, testData.user.Id)
            .ToList();
        
        Assert.Single(videos);
    }

    public void Dispose()
    {
        seedContext.Dispose();
        dbContext.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await seedContext.DisposeAsync();
        await dbContext.DisposeAsync();
    }
}