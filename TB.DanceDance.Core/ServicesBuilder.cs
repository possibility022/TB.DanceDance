using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using TB.DanceDance.Configurations;
using TB.DanceDance.Core.IdentityServerStore;
using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.MongoDb;
using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Services;
using TB.DanceDance.Services.Models;

namespace TB.DanceDance.Core
{
    public static class ServicesBuilder
    {
        public static IServiceCollection ConfigureDb(this IServiceCollection services,
            MongoDbConfiguration? mongoDbConfig = null,
            bool makeSureCollectionCreated = false)
        {
            if (mongoDbConfig == null)
                mongoDbConfig = new MongoDbConfiguration();

            return services.AddSingleton<IMongoClient>((services) =>
                 {
                     return MongoDatabaseFactory.GetClient();
                 })
                .AddSingleton((services) =>
                {
                    var mongoClient = services.GetRequiredService<IMongoClient>();
                    var db = mongoClient.GetDatabase(mongoDbConfig.Database);
                    return db;
                })
                .AddMongoCollection<ApiResourceRecord>(mongoDbConfig.ApiResourceCollection, makeSureCollectionCreated)
                .AddMongoCollection<IdentityResourceRecord>(mongoDbConfig.IdentityResourceCollection, makeSureCollectionCreated)
                .AddMongoCollection<ApiScopeRecord>(mongoDbConfig.ApiScopeCollection, makeSureCollectionCreated)
                .AddMongoCollection<UserModel>(mongoDbConfig.UserCollection, makeSureCollectionCreated)
                .AddMongoCollection<ClientRecord>(mongoDbConfig.ApiClientCollection, makeSureCollectionCreated)
                .AddMongoCollection<VideoInformation>(mongoDbConfig.VideoCollection, makeSureCollectionCreated);
        }

        public static IServiceCollection ConfigureVideoServices(this IServiceCollection services,
            BlobConfiguration? blobConfig = null,
            Func<IServiceProvider, IVideoFileLoader>? videoFileLoader = null)
        {
            if (blobConfig == null)
                blobConfig = new BlobConfiguration();

            services
                .AddSingleton<IBlobDataService>(new BlobDataService(ApplicationBlobContainerFactory.TryGetConnectionStringFromEnvironmentVariables(), blobConfig.BlobContainer))
                .AddScoped<IVideoService, VideoService>();

            if (videoFileLoader != null)
            {
                services.AddScoped<IVideoFileLoader>(videoFileLoader);
            }
            else
            {
                services.AddSingleton<IVideoFileLoader, FakeFileLoader>();
            }

            return services;
        }

        public static IServiceCollection ConfigureIdentityStorage(this IServiceCollection services)
        {
            return services
                .AddScoped<IdentityClientMongoStore>()
                .AddScoped<IdentityResourceMongoStore>();
        }
    }
}