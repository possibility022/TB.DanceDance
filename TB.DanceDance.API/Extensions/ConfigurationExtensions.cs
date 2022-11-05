using MongoDB.Driver;

namespace TB.DanceDance.API.Extensions
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddMongoCollection<T>(this IServiceCollection services, string collectionName, bool makeSureCreated = true)
        {
            return services.AddSingleton<IMongoCollection<T>>(s =>
            {
                var db = s.GetRequiredService<IMongoDatabase>();

                if (makeSureCreated)
                {
                    var col = db.ListCollectionNames()
                        .ToList();

                    if (!col.Contains(collectionName))
                    {
                        db.CreateCollection(collectionName);
                    }
                }

                var collection = db.GetCollection<T>(collectionName);
                return collection;
            });
        }
    }
}
