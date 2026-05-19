namespace TB.DanceDance.API;

public static class ApiEndpoints
{
    private const string ApiBase = "api";

    public static class Video
    {
        private const string Base = $"{ApiBase}/videos";

        public const string MyVideos = $"{Base}/my";
        public const string GetSingle = $"{Base}/{{guid}}";
        public const string GetStream = $"{Base}/{{guid}}/stream";

        public const string Rename = $"{Base}/{{videoId:guid}}/rename";
        public const string GetUploadUrl = $"{Base}/upload";
        public const string RefreshUploadUrl = $"{Base}/upload/{{videoId:guid}}";
        public const string UpdateCommentSettings = $"{Base}/{{videoId:guid}}/comment-settings";
    }

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
