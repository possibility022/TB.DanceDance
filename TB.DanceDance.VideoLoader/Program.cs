using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using TB.DanceDance.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TB.DanceDance.Core;
using MongoDB.Driver;
using TB.DanceDance.Data.MongoDb.Models;
using Microsoft.Extensions.Configuration;
using TB.DanceDance.Data.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using System.Linq;
using TB.DanceDance.Identity;
using TB.DanceDance.Core.IdentityServerStore;
using TB.DanceDance.Configurations;
using System.Threading;

namespace TB.DanceDance.VideoLoader
{
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
                services.ConfigureDb(config.GetMongoDbConfig())
                    .ConfigureVideoServices(ConnectionStringProvider.GetBlobConnectionString(config), (s) => new VideoFileLoader(FFMPGPath));

                services.AddDbContext<DanceDbContext>(options =>
                {
                    options.UseNpgsql(ConnectionStringProvider.GetPostgreSqlDbConnectionString(config));
                });

                var identityBuilder = services
                    .AddIdentityServer();

                identityBuilder
                    .AddConfigurationStore(options =>
                    {
                        options.ConfigureDbContext = b =>
                        {
                            b.UseNpgsql(ConnectionStringProvider.GetPostgreIdentityStoreDbConnectionString(config));
                        };
                    })
                    .AddOperationalStore(options =>
                    {
                        options.ConfigureDbContext = b =>
                        {
                            b.UseNpgsql(ConnectionStringProvider.GetPostgreIdentityStoreDbConnectionString(config));
                        };
                    });
            });

            ConfigureLogging();
            builder.UseSerilog();

            app = builder.Build();
            using var scope = app.Services.CreateScope();

            var identityConfigDbScope = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            var mongodb = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();

            await SetBasicIdentityConfiguration(identityConfigDbScope, mongodb);

            return;

            var dbMig = new DbMigration(
                scope.ServiceProvider.GetRequiredService<DanceDbContext>(),
                scope.ServiceProvider.GetRequiredService<IMongoCollection<VideoInformation>>(),
                scope.ServiceProvider.GetRequiredService<IMongoCollection<Group>>(),
                scope.ServiceProvider.GetRequiredService<IMongoCollection<Event>>());

            await dbMig.MigrateAsync();


            //await CreateOwnersAsync();
            //await SetVideoOwnerAsync();

            // await SetUsersAccounts(app.Services.GetRequiredService<IUserService>());
            // return;

            //var service = scope.ServiceProvider.GetRequiredService<IVideoService>();

            //var files = Directory.GetFiles("E:\\NONONOWestCoastSwing", "*.webm");
            //var videos = app.Services.GetRequiredService<IMongoCollection<VideoInformation>>();

            //foreach (var file in files)
            //{
            //    Log.Information("Uploading {0}", file);
            //    var uploaded = await service.UploadVideoAsync(file, CancellationToken.None);
            //    uploaded.SharedWith = new SharingScope()
            //    {
            //        EntityId = Constants.GroupSroda1730,
            //        Assignment = AssignmentType.Group
            //    };

            //    await videos.ReplaceOneAsync(s => s.Id == uploaded.Id, uploaded);

            //}

            //Log.Information("Done");
        }

        private static async Task SetVideoOwnerAsync()
        {
            var videos = app.Services.GetRequiredService<IMongoCollection<VideoInformation>>();
            var find = await videos.FindAsync(FilterDefinition<VideoInformation>.Empty);
            var videosList = find.ToList();

            foreach (var videoInformation in videosList)
            {
                if (videoInformation.Name.Contains("Footworki"))
                {
                    videoInformation.SharedWith = new SharingScope()
                    {
                        EntityId = Constants.WarsztatyFootworki2022,
                        Assignment = AssignmentType.Event
                    };
                }
                else if (videoInformation.Name.Contains("Rama"))
                {
                    videoInformation.SharedWith = new SharingScope()
                    {
                        EntityId = Constants.WarsztatyRama2022,
                        Assignment = AssignmentType.Event
                    };
                }
                else
                {
                    videoInformation.SharedWith = new SharingScope()
                    {
                        EntityId = Constants.GroupSroda1730,
                        Assignment = AssignmentType.Group
                    };
                }

                await videos.ReplaceOneAsync((s) => s.Id == videoInformation.Id, videoInformation);
            }
        }

        private static async Task CreateOwnersAsync()
        {
            var attenders = new List<string>() { TomekUserId };

            var events = app.Services.GetRequiredService<IMongoCollection<Event>>();
            await events.InsertManyAsync(new[]
            {
                new Event()
                {
                    Id = Constants.WarsztatyFootworki2022,
                    Attenders = attenders,
                    Date = new DateTimeOffset(DateTime.Parse("2022-07-01T22:00:00.000+00:00")),
                    Name = "Warsztaty - Footworki - 2022"
                },
                new Event()
                {
                    Id = Constants.WarsztatyRama2022,
                    Attenders = attenders,
                    Date = new DateTimeOffset(DateTime.Parse("2022-09-30T22:00:00.000+00:00")),
                    Name = "Warsztaty - Rama - 2022"
                }
            });

            var groups = app.Services.GetRequiredService<IMongoCollection<Group>>();
            await groups.InsertManyAsync(new[]
            {
                new Group()
                {
                    Id = Constants.GroupSroda1730,
                    People = attenders,
                    GroupName = "Środy 17:30"
                }
            });
        }

        //private static Task SetUsersAccounts(IUserService userService)
        //{
        //    var address = new
        //    {
        //        street_address = "Somwhere",
        //        locality = "In",
        //        postal_code = 1337,
        //        country = "Poland"
        //    };

        //    return userService.AddUpsertUserAsync(new Services.Models.UserModel()
        //    {

        //        SubjectId = "818727",
        //        Username = "alice",
        //        Password = "alice",
        //        Claims =
        //                {
        //                    new Claim(JwtClaimTypes.Name, "Alice Smith"),
        //                    new Claim(JwtClaimTypes.GivenName, "Alice"),
        //                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
        //                    new Claim(JwtClaimTypes.Email, "AliceSmith@email.com"),
        //                new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
        //                    new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
        //                    new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json)
        //                }

        //    });
        //}

        public static async Task SetBasicIdentityConfiguration(ConfigurationDbContext context,
            IMongoDatabase database)
        {
            var config = new MongoDbConfiguration();

            IMongoCollection<ClientRecord> clientRecords = database.GetCollection<ClientRecord>(config.ApiClientCollection);
            IMongoCollection<ApiResourceRecord> apiResourceCollection = database.GetCollection<ApiResourceRecord>(config.ApiResourceCollection);
            IMongoCollection<ApiScopeRecord> apiScopeCollection = database.GetCollection<ApiScopeRecord>(config.ApiScopeCollection);
            IMongoCollection<IdentityResourceRecord> identityResourceCollection = database.GetCollection<IdentityResourceRecord>(config.IdentityResourceCollection);

            Console.WriteLine("Are you sure to update configuration? This may override current settings and break application. y/N");

            var input = Console.ReadLine();
            if (input?.Equals("y", StringComparison.InvariantCultureIgnoreCase) != true)
                return;

            Console.WriteLine("Migrate Production? y/N");
            input = Console.ReadLine();
            bool migrateProduction = input?.Equals("y", StringComparison.InvariantCultureIgnoreCase) == true;

            if (migrateProduction)
            {
                if (!context.Clients.Any())
                {
                    var clients = await clientRecords
                        .Find((r) => true)
                        .ToListAsync();
                    foreach (var client in clients)
                    {
                        var cast = (IdentityServer4.Models.Client)client;
                        context.Clients.Add(cast.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    var identityResources = await identityResourceCollection
                        .Find((c) => true)
                        .ToListAsync();
                    foreach (var resource in identityResources)
                    {
                        var cast = (IdentityServer4.Models.IdentityResource)resource;
                        context.IdentityResources.Add(cast.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiScopes.Any())
                {
                    var apiScopes = await apiScopeCollection
                        .Find((r) => true)
                        .ToListAsync();

                    foreach (var resource in apiScopes)
                    {
                        var cast = (IdentityServer4.Models.ApiScope)resource;
                        context.ApiScopes.Add(cast.ToEntity());
                    }
                    context.SaveChanges();
                }
            }
            else
            {

                if (!context.Clients.Any())
                {
                    foreach (var client in Config.Clients)
                    {
                        var cast = (IdentityServer4.Models.Client)client;
                        context.Clients.Add(cast.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in Config.GetIdentityResources())
                    {
                        var cast = (IdentityServer4.Models.IdentityResource)resource;
                        context.IdentityResources.Add(cast.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiScopes.Any())
                {
                    foreach (var resource in Config.ApiScopes)
                    {
                        var cast = (IdentityServer4.Models.ApiScope)resource;
                        context.ApiScopes.Add(cast.ToEntity());
                    }
                    context.SaveChanges();
                }
            }
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
