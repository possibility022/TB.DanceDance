namespace TB.DanceDance.API;

public static class ApiEndpoints
{
    private const string ApiBase = "api";

    public static class Video
    {
        private const string Base = $"{ApiBase}/video"; //todo to plural

        public const string GetAll = $"{Base}/getinformation";
        public const string GetSingle = $"{Base}/{{guid}}/getinformation";
        public const string GetStream = $"{Base}/stream/{{guid}}";

        public const string Rename = $"{Base}/{{guid}}/rename";
        public const string GetUploadUrl = $"{Base}/getUploadUrl";


        public static class Access
        {
            private const string Base = $"{Video.Base}/access";

            public const string GetAll = $"{Base}/getall";
            public const string GetUserAccess = $"{Base}/user";
            public const string RequestAccess = $"{Base}/request";
        }

    }

    public static class Info
    {
        public const string AllEndpoints = "/.endpoints";
    }
}
