namespace TB.DanceDance.Configurations
{
    public class MongoDbConfiguration
    {

        public string? ConnectionString { get; set; }
        public string Database => "danceDb";
        public string VideoCollection => "videoCollection";
        public string Events => "events";
        public string Groups => "groups";
        public string SharedVideos => "toConvert";
        public string ApiResourceCollection => "apiResource";
        public string ApiScopeCollection => "apiScope";
        public string RequestedAssignmentCollection => "requestedAssignment";
        public string IdentityResourceCollection => "identityResource";
        public string UserCollection => "users";
        public string ApiClientCollection => "apiClients";
    }
}
