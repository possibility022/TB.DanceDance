public static class Routes
{
    public static class Events
    {
        private const string EVENT = "event";
        public const string EventsList = EVENT;
        public const string EventDetails = $"{EVENT}Details";
    }
    
    public static class Groups
    {
        private const string GROUP = "group";
        public const string AllVideos = GROUP;
    }
    
    public const string Player= "player";
    
    public static class Upload
    {
        private const string BASE = "upload";
        public const string Uploader = $"{BASE}Uploader";
        public const string Manager = $"{BASE}Manager";
    }
}