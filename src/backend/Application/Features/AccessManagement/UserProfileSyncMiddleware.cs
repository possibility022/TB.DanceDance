using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Application.Features.AccessManagement;

public class UserProfileSyncMiddleware(RequestDelegate next, IMemoryCache cache)
{
    public async Task InvokeAsync(
        HttpContext context,
        IAccessManagementService accessManagement,
        IIdentityClient identityClient,
        ILogger<UserProfileSyncMiddleware> logger)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId) && !cache.TryGetValue(CacheKey(userId), out _))
            {
                try
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    if (!string.IsNullOrEmpty(authHeader))
                    {
                        var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                            ? authHeader["Bearer ".Length..].Trim()
                            : authHeader;

                        var userData = await identityClient.GetNameAsync(token, context.RequestAborted);
                        await accessManagement.FillMissingUserDataAsync(userData, context.RequestAborted);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to sync profile for user {UserId}", userId);
                }
                finally
                {
                    cache.Set(CacheKey(userId), true, TimeSpan.FromHours(1));
                }
            }
        }

        await next(context);
    }

    private static string CacheKey(string userId) => $"profile_synced:{userId}";
}
