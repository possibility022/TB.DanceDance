using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading.Tasks;
using TB.DanceDance.Core;
using TB.DanceDance.Data.PostgreSQL;
using TB.DanceDance.Identity.Extensions;
using TB.DanceDance.Services;

namespace TB.DanceDance.VideoLoader;

class Program
{
    const string FFMPGPath = @"D:\Programy\ffmpeg-2022-12-04-git-6c814093d8-full_build\bin\ffmpeg.exe";

    private const string TomekUserId = "1234567890123";

    private static IHost app;

    static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder();

        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets("76b0dd76-61c4-4a28-a39f-109d587bd5c0") // This is the same as in API Project
                                                                    //.AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables();

        var config = configurationBuilder.Build();

        builder.ConfigureServices((context, services) =>
        {
            services
                .ConfigureVideoServices(ConnectionStringProvider.GetBlobConnectionString(config), (s) => new VideoFileLoader(FFMPGPath));

            services.AddDbContext<DanceDbContext>(options =>
            {
                options.UseNpgsql(ConnectionStringProvider.GetPostgreSqlDbConnectionString(config));
            });

            var identityBuilder = services
                .AddIdentityServer();

            identityBuilder
                .RegisterIdenityServerStorage(ConnectionStringProvider.GetPostgreIdentityStoreDbConnectionString(config));
        });

        ConfigureLogging();
        builder.UseSerilog();

        app = builder.Build();
        using var scope = app.Services.CreateScope();

        var identityConfigDbScope = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();


        //Log.Information("Done");
    }





    private static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File($"log-{DateTime.UtcNow.ToString("s").Replace(":", "")}.txt", rollingInterval: RollingInterval.Infinite)
            .CreateLogger();
    }
}
