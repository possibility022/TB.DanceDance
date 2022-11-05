using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace TB.DanceDance.Core
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddMongoCollection<T>(this IServiceCollection services,
            string collectionName,
            bool makeSureCreated = false)
        {
            return services.AddSingleton(s =>
            {
                var db = s.GetRequiredService<IMongoDatabase>();

                if (makeSureCreated)
                {
                    // todo - make that record id is confgured here, not by properties and attributes.

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
