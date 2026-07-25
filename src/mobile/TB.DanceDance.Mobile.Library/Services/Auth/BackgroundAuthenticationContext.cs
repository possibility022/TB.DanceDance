namespace TB.DanceDance.Mobile.Library.Services.Auth;

public sealed class BackgroundAuthenticationRequiredException()
    : InvalidOperationException("Interactive authentication is required.");

public static class BackgroundAuthenticationContext
{
    private static readonly AsyncLocal<bool> SilentAuthentication = new();

    public static bool IsSilentAuthenticationRequired => SilentAuthentication.Value;

    public static IDisposable RequireSilentAuthentication()
    {
        var previous = SilentAuthentication.Value;
        SilentAuthentication.Value = true;
        return new Scope(previous);
    }

    private sealed class Scope(bool previous) : IDisposable
    {
        public void Dispose() => SilentAuthentication.Value = previous;
    }
}
