namespace TB.DanceDance.API;

public static class ApiEndpoints
{
    private const string ApiBase = "api";

    public static class Converter
    {
        private const string Base = $"{ApiBase}/converter";

        public const string Videos = $"{Base}/videos";
        public const string Upload = $"{Base}/videos/{{videoId}}/publish";
        public const string GetPublishSas = $"{Base}/videos/{{videoId}}/sas";
    }
    
    public static class Info
    {
        public const string AllEndpoints = "/.endpoints";
    }
}
