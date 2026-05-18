namespace TB.DanceDance.API.Features.Videos;

public static class VideoRoutes
{
    private const string ApiBase = "api";
    private const string Base = $"{ApiBase}/videos";

    public const string MyVideos = $"{Base}/my";
    public const string GetSingle = $"{Base}/{{guid}}";
    public const string GetStream = $"{Base}/{{guid}}/stream";
    public const string Rename = $"{Base}/{{videoId:guid}}/rename";
    public const string GetUploadUrl = $"{Base}/upload";
    public const string RefreshUploadUrl = $"{Base}/upload/{{videoId:guid}}";
    public const string UpdateCommentSettings = $"{Base}/{{videoId:guid}}/comment-settings";
}
