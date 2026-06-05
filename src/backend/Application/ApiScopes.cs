using TB.DanceDance.API.Contracts.ApiResources;

namespace Application;

/// <summary>
/// Authorization policy names for FastEndpoints' <c>Policies(...)</c>, mirroring the scopes the old
/// controllers required via <c>[Authorize(...)]</c>. The policies themselves are registered in the
/// API host (Program.cs). Values are re-exported from <see cref="DanceDanceResources"/> so there is a
/// single source of truth for the scope strings.
/// </summary>
public static class ApiScopes
{
    public const string Read = DanceDanceResources.WestCoastSwing.Scopes.ReadScope;
    public const string Convert = DanceDanceResources.WestCoastSwing.Scopes.WriteConvert;
}
