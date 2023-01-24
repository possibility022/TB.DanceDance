namespace TB.DanceDance.Configurations
{
    public class MongoDbConfiguration
    {
        public const string ConnectionStringName = "MongoDB";

        public string ConnectionString { get; set; }

        public string Database { get; set; } = "danceDb";
        public string VideoCollection { get; set; } = "videoCollection";
        public string OwnersCollection { get; set; } = "owners";
        public string ApiResourceCollection { get; set; } = "apiResource";
        public string ApiScopeCollection { get; set; } = "apiScope";
        public string IdentityResourceCollection { get; set; } = "identityResource";
        public string UserCollection { get; set; } = "users";
        public string ApiClientCollection { get; set; } = "apiClients";
    }
}
