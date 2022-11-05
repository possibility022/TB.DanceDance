namespace TB.DanceDance.Configurations
{
    public class MongoDbConfiguration
    {
        public string Database { get; set; } = "danceDb";
        public string VideoCollection { get; set; } = "videoCollection";
        public string ApiResourceCollection { get; set; } = "apiResource";
        public string ApiScopeCollection { get; set; } = "apiScope";
        public string IdentityResourceCollection { get; set; } = "identityResource";
        public string UserCollection { get; set; } = "users";
        public string ApiClientCollection { get; set; } = "apiClients";
    }
}
