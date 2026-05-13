namespace TB.Auth.Web;

public static class ScopeParser
{
    internal static IEnumerable<string> ParseScopes(string? scope)
    {
        return string.IsNullOrWhiteSpace(scope)
            ? []
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}