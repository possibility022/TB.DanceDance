using TB.DanceDance.API.Contracts.Features.AccessManagement;
using TB.DanceDance.API.Contracts.Features.AccessManagement.Models;
using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class UserAccessCacheTests
{
    [Fact]
    public async Task ConcurrentCallersShareOneFetch()
    {
        var cache = new UserAccessCache();
        var fetchCount = 0;
        var response = new GetUserAccessResponse
        {
            Assigned = new GetUserAccessSet(),
            Available = new GetUserAccessSet(),
            Pending = new ListUserAccessPending()
        };

        async Task<GetUserAccessResponse> Fetch(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref fetchCount);
            await Task.Delay(20, cancellationToken);
            return response;
        }

        var results = await Task.WhenAll(
            Enumerable.Range(0, 10)
                .Select(_ => cache.GetOrCreateAsync(Fetch, TestContext.Current.CancellationToken)));

        Assert.Equal(1, fetchCount);
        Assert.All(results, result => Assert.Same(response, result));
    }

    [Fact]
    public async Task InvalidateForcesNextFetch()
    {
        var cache = new UserAccessCache();
        var fetchCount = 0;

        Task<GetUserAccessResponse> Fetch(CancellationToken _)
        {
            Interlocked.Increment(ref fetchCount);
            return Task.FromResult(new GetUserAccessResponse
            {
                Assigned = new GetUserAccessSet(),
                Available = new GetUserAccessSet(),
                Pending = new ListUserAccessPending()
            });
        }

        await cache.GetOrCreateAsync(Fetch, TestContext.Current.CancellationToken);
        cache.Invalidate();
        await cache.GetOrCreateAsync(Fetch, TestContext.Current.CancellationToken);

        Assert.Equal(2, fetchCount);
    }
}
