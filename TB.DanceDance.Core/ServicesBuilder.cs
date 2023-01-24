using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using TB.DanceDance.Configurations;
using TB.DanceDance.Core.IdentityServerStore;
using TB.DanceDance.Data.Blobs;
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

            return services.AddSingleton((services) =>
                    {
                        var mongoClient = services.GetRequiredService<IMongoClient>();
                        var db = mongoClient.GetDatabase(mongoDbConfig.Database);
                        return db;
                    })
                    .AddMongoCollection<ApiResourceRecord>(mongoDbConfig.ApiResourceCollection,
                        makeSureCollectionCreated)
                    .AddMongoCollection<IdentityResourceRecord>(mongoDbConfig.IdentityResourceCollection,
                        makeSureCollectionCreated)
                    .AddMongoCollection<ApiScopeRecord>(mongoDbConfig.ApiScopeCollection, makeSureCollectionCreated)
                    .AddMongoCollection<UserModel>(mongoDbConfig.UserCollection, makeSureCollectionCreated)
                    .AddMongoCollection<ClientRecord>(mongoDbConfig.ApiClientCollection, makeSureCollectionCreated)
                    .AddMongoCollection<VideoInformation>(mongoDbConfig.VideoCollection, makeSureCollectionCreated)
                    .AddMongoCollection<Event>(mongoDbConfig.OwnersCollection)
                    .AddMongoCollection<Group>(mongoDbConfig.OwnersCollection)
                ;
        }

        public static IServiceCollection ConfigureVideoServices(this IServiceCollection services,
            Func<IServiceProvider, IVideoFileLoader>? videoFileLoader = null)
        {
            services
                .AddSingleton<IBlobDataServiceFactory>(r => new BlobDataServiceFactory(ApplicationBlobContainerFactory.TryGetConnectionStringFromEnvironmentVariables()))
                .AddScoped<IVideoService, VideoService>()
                .AddScoped<IVideoUploaderService, VideoUploaderService>();

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