using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace TB.DanceDance.Core
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddMongoCollection<T>(this IServiceCollection services,
            string collectionName,
            string? idProperty = null,
            bool makeSureCreated = false)
        {
            return services.AddSingleton(s =>
            {
                var db = s.GetRequiredService<IMongoDatabase>();

                if (makeSureCreated)
                {
                    var col = db
                        .ListCollectionNames()
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
