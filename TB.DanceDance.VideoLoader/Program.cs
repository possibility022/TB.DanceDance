using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TB.DanceDance.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TB.DanceDance.Core;
using TB.DanceDance.Core.IdentityServerStore;

namespace TB.DanceDance.VideoLoader
{
    class Program
    {
        const string FFMPGPath = @"C:\Users\TomaszBak\Downloads\ffmpeg-master-latest-win64-gpl\bin\ffmpeg.exe";

        static async Task Main(string[] args)
        {
            var buidler = Host.CreateDefaultBuilder();

            buidler.ConfigureServices(services =>
            {
                services.ConfigureDb()
                    .ConfigureVideoServices(null, (s) => new VideoFileLoader(FFMPGPath))
                    .ConfigureIdentityStorage();
            });

            buidler.UseSerilog();


            var app = buidler.Build();

            await SetBasicIdentityConfiguration(app.Services.GetRequiredService<IdentityResourceMongoStore>(), app.Services.GetRequiredService<IdentityClientMongoStore>());
            return;

            var service = app.Services.GetRequiredService<IVideoService>();

            var files = Directory.GetFiles("G:\\West\\WebM2");

            foreach (var file in files)
            {
                Log.Information("Uploading {0}", file);
                await service.UploadVideoAsync(file, CancellationToken.None);
            }

            Log.Information("Done");
        }

        public static async Task SetBasicIdentityConfiguration(IdentityResourceMongoStore resources, IdentityClientMongoStore clientStore)
        {
            foreach (var ir in Config.GetIdentityResources())
                await resources.AddIdentityResource(ir);

            foreach (var apiRes in Config.ApiResources)
                await resources.AddApiResource(apiRes);

            foreach (var apiScope in Config.ApiScopes)
                await resources.AddApiScopeAsync(apiScope);

            foreach (var client in Config.Clients)
                await clientStore.AddClientAsync(client);
        }

        private static void ConfigureLogging(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File($"log-{DateTime.UtcNow.ToString("s").Replace(":", "")}.txt", rollingInterval: RollingInterval.Infinite)
                .CreateLogger();
        }
    }
}
