namespace TB.DanceDance.API.Features.Conversion;

public static class ConversionRoutes
{
    private const string ApiBase = "api";
    private const string Base = $"{ApiBase}/converter";

    public const string Videos = $"{Base}/videos";
    public const string Upload = $"{Base}/videos/{{videoId}}/publish";
    public const string GetPublishSas = $"{Base}/videos/{{videoId}}/sas";
}
