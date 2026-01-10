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


        public static class Access
        {
            private const string Base = $"{Video.Base}/accesses";

            public const string GetAll = $"{Base}";
            public const string GetUserAccess = $"{Base}/my";
            public const string RequestAccess = $"{Base}/request";
            public const string ManageAccessRequests = $"{Base}/requests";
        }

    }

    public static class Group
    {
        private const string Base = $"{ApiBase}/groups";

        public const string Videos = $"{Base}/videos";
        public const string VideosForGroup = $"{Base}/{{groupId:guid}}/videos";
    }

    public static class Event
    {
        private const string Base = $"{ApiBase}/events";

        public const string AddEvent = $"{Base}";
        public const string Videos = $"{Base}/{{eventId:guid}}/videos";
    }

    public static class Converter
    {
        private const string Base = $"{ApiBase}/converter";

        public const string Videos = $"{Base}/videos";
        public const string Upload = $"{Base}/videos/{{videoId}}/publish";
        public const string GetPublishSas = $"{Base}/videos/{{videoId}}/sas";
    }

    public static class Share
    {
        private const string Base = $"{ApiBase}/share";

        public const string Create = $"{ApiBase}/videos/{{videoId:guid}}/share";
        public const string Revoke = $"{Base}/{{linkId}}";
        public const string GetMy = $"{Base}/my";
        public const string GetInfo = $"{Base}/{{linkId}}";
        public const string GetStream = $"{Base}/{{linkId}}/stream";
    }

    public static class Comments
    {
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

    public static class Info
    {
        public const string AllEndpoints = "/.endpoints";
    }
}
