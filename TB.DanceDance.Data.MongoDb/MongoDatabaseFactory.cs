using MongoDB.Driver;

namespace TB.DanceDance.Data.MongoDb
{
    public class MongoDatabaseFactory
    {
        public static MongoClient GetClient()
        {
            var connectionString = Environment.GetEnvironmentVariable("TB.DanceDance.ConnectionString.Mongo");
            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("Connection string not available in environment variables");

            var client = new MongoClient(connectionString);
            return client;
        }
    }
}
