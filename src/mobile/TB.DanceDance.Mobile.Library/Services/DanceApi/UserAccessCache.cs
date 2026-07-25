using TB.DanceDance.API.Contracts.Features.AccessManagement;

namespace TB.DanceDance.Mobile.Library.Services.DanceApi;

public sealed class UserAccessCache
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);
    private readonly SemaphoreSlim gate = new(1, 1);
    private GetUserAccessResponse? value;
    private DateTimeOffset expiresAt;

    public async Task<GetUserAccessResponse> GetOrCreateAsync(
        Func<CancellationToken, Task<GetUserAccessResponse>> factory,
        CancellationToken cancellationToken)
    {
        if (value is not null && expiresAt > DateTimeOffset.UtcNow)
            return value;

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (value is not null && expiresAt > DateTimeOffset.UtcNow)
                return value;

            value = await factory(cancellationToken);
            expiresAt = DateTimeOffset.UtcNow.Add(Lifetime);
            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Invalidate()
    {
        value = null;
        expiresAt = default;
    }
}
