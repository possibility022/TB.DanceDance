namespace TB.DanceDance.API.Features.Comments;

public static class CommentRoutes
{
    private const string ApiBase = "api";
    private const string Base = $"{ApiBase}/comments";

    public const string GetCommentsForVideo = $"{Base}/video/{{videoId:guid}}";
    public const string Create = $"{ApiBase}/share/{{linkId}}/comments";
    public const string GetByLink = $"{ApiBase}/share/{{linkId}}/comments";
    public const string Update = $"{Base}/{{commentId:guid}}";
    public const string Delete = $"{Base}/{{commentId:guid}}";
    public const string Hide = $"{Base}/{{commentId:guid}}/hide";
    public const string Unhide = $"{Base}/{{commentId:guid}}/unhide";
    public const string Report = $"{Base}/{{commentId:guid}}/report";
}
