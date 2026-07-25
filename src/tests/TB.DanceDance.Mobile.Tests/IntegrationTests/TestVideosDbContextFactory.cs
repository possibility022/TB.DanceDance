using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Mobile.Library.Data;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

internal sealed class TestVideosDbContextFactory : IDbContextFactory<VideosDbContext>
{
    private readonly DbContextOptions<VideosDbContext> options;

    public TestVideosDbContextFactory()
    {
        options = new DbContextOptionsBuilder<VideosDbContext>()
            .UseInMemoryDatabase($"videos-{Guid.NewGuid()}")
            .Options;
    }

    public VideosDbContext CreateDbContext() => new(options);

    public Task<VideosDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());
}
