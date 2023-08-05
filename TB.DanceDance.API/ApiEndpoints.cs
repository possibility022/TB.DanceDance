namespace TB.DanceDance.API;

public static class ApiEndpoints
{
    private const string ApiBase = "api";

    public static class Video
    {
        private const string Base = $"{ApiBase}/videos";

        public const string GetAll = $"{Base}";
        public const string GetSingle = $"{Base}/{{guid}}";
        public const string GetStream = $"{Base}/{{guid}}/stream";

        public const string Rename = $"{Base}/{{guid}}/rename";
        public const string GetUploadUrl = $"{Base}/upload";


        public static class Access
        {
            private const string Base = $"{Video.Base}/accesses";

            public const string GetAll = $"{Base}";
            public const string GetUserAccess = $"{Base}/my";
            public const string RequestAccess = $"{Base}/request";
        }

    }

    public static class Group
    {
        private const string Base = $"{ApiBase}/groups";

        public const string Videos = $"{Base}/videos";
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

    public static class Info
    {
        public const string AllEndpoints = "/.endpoints";
    }
}
