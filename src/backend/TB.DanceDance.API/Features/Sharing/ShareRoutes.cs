namespace TB.DanceDance.API.Features.Sharing;

public static class ShareRoutes
{
    private const string ApiBase = "api";
    private const string Base = $"{ApiBase}/share";

    public const string Create = $"{ApiBase}/videos/{{videoId:guid}}/share";
    public const string Revoke = $"{Base}/{{linkId}}";
    public const string GetMy = $"{Base}/my";
    public const string GetInfo = $"{Base}/{{linkId}}";
    public const string GetStream = $"{Base}/{{linkId}}/stream";
}
