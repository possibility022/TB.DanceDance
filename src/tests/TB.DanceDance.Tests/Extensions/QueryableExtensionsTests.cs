using Application.Extensions;
using Domain.Entities;
using Infrastructure.Data;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Extensions;

public class QueryableExtensionsTests : BaseTestClass
{
    private DanceDbContext runtimeDbContext = null!;

    public QueryableExtensionsTests(DanceDbFixture dbContextFixture) : base(dbContextFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        this.runtimeDbContext = runtimeDbContext;
        return ValueTask.CompletedTask;
    }

    private async Task<string> SeedVideosAsync(int count)
    {
        var user = new UserDataBuilder().Build();
        var videos = Enumerable.Range(0, count)
            .Select(i => new VideoDataBuilder()
                .OwnedBy(user)
                .WithName($"Video{i:D2}")
                .RecordedAt(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i))
                .Build())
            .ToArray();

        SeedDbContext.Add(user);
        SeedDbContext.AddRange(videos);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return user.Id;
    }

    private IQueryable<Video> QueryFor(string userId) =>
        runtimeDbContext.Videos
            .Where(v => v.OwnerUserId == userId)
            .OrderBy(v => v.RecordedDateTime);

    // Q1: Returns the requested page slice and the total count across all pages
    [Fact]
    public async Task ToPagedResultAsync_ReturnsRequestedPage_AndTotalCount()
    {
        var userId = await SeedVideosAsync(5);

        var (items, totalCount) = await QueryFor(userId).ToPagedResultAsync(pageNumber: 2, pageSize: 2, TestContext.Current.CancellationToken);

        Assert.Equal(5, totalCount);
        Assert.Equal(["Video02", "Video03"], items.Select(v => v.Name));
    }

    // Q2: A partial last page returns only the remaining items, with the correct total count
    [Fact]
    public async Task ToPagedResultAsync_ReturnsPartialLastPage()
    {
        var userId = await SeedVideosAsync(5);

        var (items, totalCount) = await QueryFor(userId).ToPagedResultAsync(pageNumber: 3, pageSize: 2, TestContext.Current.CancellationToken);

        Assert.Equal(5, totalCount);
        Assert.Equal(["Video04"], items.Select(v => v.Name));
    }

    // Q3: A page beyond the last one returns no items but still reports the correct total count
    [Fact]
    public async Task ToPagedResultAsync_ReturnsEmptyItems_ButCorrectTotalCount_WhenPageOutOfRange()
    {
        var userId = await SeedVideosAsync(3);

        var (items, totalCount) = await QueryFor(userId).ToPagedResultAsync(pageNumber: 5, pageSize: 2, TestContext.Current.CancellationToken);

        Assert.Equal(3, totalCount);
        Assert.Empty(items);
    }

    // Q4: An empty source returns no items and a zero total count
    [Fact]
    public async Task ToPagedResultAsync_ReturnsEmpty_WhenSourceIsEmpty()
    {
        var userId = await SeedVideosAsync(0);

        var (items, totalCount) = await QueryFor(userId).ToPagedResultAsync(pageNumber: 1, pageSize: 10, TestContext.Current.CancellationToken);

        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }
}
