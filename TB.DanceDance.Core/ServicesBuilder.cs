using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using TB.DanceDance.Configurations;
using TB.DanceDance.Core.IdentityServerStore;
using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Identity;
using TB.DanceDance.Services;

namespace TB.DanceDance.Core
{
    public static class ServicesBuilder
    {
        public static IServiceCollection ConfigureDb(this IServiceCollection services,
            MongoDbConfiguration mongoDbConfig,
            bool makeSureCollectionCreated = false)
        {

            return services.AddSingleton<IMongoClient>((services) =>
                    {
                        return new MongoClient(mongoDbConfig.ConnectionString);
                    })
                    .AddSingleton((services) =>
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
                    .AddMongoCollection<Event>(mongoDbConfig.Events)
                    .AddMongoCollection<Group>(mongoDbConfig.Groups)
                    .AddMongoCollection<RequestedAssigment>(mongoDbConfig.RequestedAssignmentCollection)
                    .AddMongoCollection<SharedVideo>(mongoDbConfig.SharedVideos)
                ;
        }

        public static MongoDbConfiguration GetMongoDbConfig(this IConfiguration configuration)
        {
            var cs = ConnectionStringProvider.GetMongoDbConnectionString(configuration);

            return new MongoDbConfiguration()
            {
                ConnectionString = cs
            };
        }

        public static IServiceCollection ConfigureVideoServices(this IServiceCollection services,
            string connectionString,
            Func<IServiceProvider, IVideoFileLoader>? videoFileLoader = null)
        {

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            services
                .AddSingleton<IBlobDataServiceFactory>(r => new BlobDataServiceFactory(connectionString))
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