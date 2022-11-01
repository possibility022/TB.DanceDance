using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TB.DanceDance.Configurations;
using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.MongoDb;
using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Services;

namespace TB.DanceDance.VideoLoader
{
    class Program
    {
        const string FFMPGPath = @"C:\Users\TomaszBak\Downloads\ffmpeg-master-latest-win64-gpl\bin\ffmpeg.exe";

        static async Task Main(string[] args)
        {
            ConfigureLogging();

            var mongoConfig = new MongoDbConfiguration();
            var blobConfig = new BlobConfiguration();

            var mongoClient = MongoDatabaseFactory.GetClient();
            var db = mongoClient.GetDatabase(mongoConfig.Database);
            

            var collection = db.GetCollection<VideoInformation>(mongoConfig.VideoCollection);

            var blobService = new BlobDataService(ApplicationBlobContainerFactory.TryGetConnectionStringFromEnvironmentVariables(), blobConfig.BlobContainer);

            var videoFileLoader = new VideoFileLoader(FFMPGPath);

            var service = new VideoService(collection, blobService, videoFileLoader);

            var files = Directory.GetFiles("G:\\West\\WebM2");

            foreach (var file in files)
            {
                Log.Information("Uploading {0}", file);
                await service.UploadVideoAsync(file, CancellationToken.None);
            }

            Log.Information("Done");
        }

        private static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File($"log-{DateTime.UtcNow.ToString("s").Replace(":", "")}.txt", rollingInterval: RollingInterval.Infinite)
                .CreateLogger();
        }
    }
}
