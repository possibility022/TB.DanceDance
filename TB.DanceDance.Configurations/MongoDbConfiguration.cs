namespace TB.DanceDance.Configurations
{
    public class MongoDbConfiguration
    {
        public string Database => "danceDb";
        public string VideoCollection => "videoCollection";
        public string OwnersCollection => "owners";
        public string ApiResourceCollection => "apiResource";
        public string ApiScopeCollection => "apiScope";
        public string IdentityResourceCollection => "identityResource";
        public string UserCollection => "users";
        public string ApiClientCollection => "apiClients";
    }
}
