namespace TB.DanceDance.API;

public static class ApiEndpoints
{
    private const string ApiBase = "api";

    public static class Video
    {
        private const string Base = $"{ApiBase}/videos"; //todo to plural

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

    public static class Converter
    {
        private const string Base = $"{ApiBase}/converter";

        public const string GetVideo = $"{Base}/video";
        public const string UpdateInfo = $"{Base}/video";
    }

    public static class Info
    {
        public const string AllEndpoints = "/.endpoints";
    }
}
