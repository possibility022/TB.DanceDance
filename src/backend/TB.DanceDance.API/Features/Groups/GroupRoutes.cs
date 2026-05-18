namespace TB.DanceDance.API.Features.Groups;

public static class GroupRoutes
{
    private const string ApiBase = "api";
    private const string Base = $"{ApiBase}/groups";

    public const string Videos = $"{Base}/videos";
    public const string VideosForGroup = $"{Base}/{{groupId:guid}}/videos";
}
