namespace Application;

public static class ApiRoutes
{
    private const string ApiBase = "api";

    public static class Groups
    {
        private const string Base = $"{ApiBase}/groups";

        public const string Videos = $"{Base}/videos";
        public const string VideosForGroup = $"{Base}/{{groupId:guid}}/videos";
    }
    
    public static class Events
    {
        private const string Base = $"{ApiBase}/events";

        public const string AddEvent = $"{Base}";
        public const string Videos = $"{Base}/{{eventId:guid}}/videos";
    }
}
