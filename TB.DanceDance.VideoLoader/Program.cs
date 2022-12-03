using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using TB.DanceDance.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TB.DanceDance.Core;
using TB.DanceDance.Core.IdentityServerStore;
using IdentityModel;
using IdentityServer4;
using System.Security.Claims;
using System.Text.Json;
using MongoDB.Driver;
using TB.DanceDance.Data.MongoDb.Models;

namespace TB.DanceDance.VideoLoader
{
    class Program
    {
        const string FFMPGPath = @"C:\Users\TomaszBak\Downloads\ffmpeg-master-latest-win64-gpl\bin\ffmpeg.exe";

        private const string TomekUserId = "1234567890123";

        private static IHost app;

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


            app = buidler.Build();

            //await CreateOwnersAsync();
            await SetVideoOwnerAsync();

            // await SetUsersAccounts(app.Services.GetRequiredService<IUserService>());
            // return;

            // var service = app.Services.GetRequiredService<IVideoService>();
            //
            // var files = Directory.GetFiles("G:\\West\\WebM2");
            //
            // foreach (var file in files)
            // {
            //     Log.Information("Uploading {0}", file);
            //     await service.UploadVideoAsync(file, CancellationToken.None);
            // }
            //
            // Log.Information("Done");
        }

        private static async Task SetVideoOwnerAsync()
        {
            var videos = app.Services.GetRequiredService<IMongoCollection<VideoInformation>>();
            var find  = await videos.FindAsync(FilterDefinition<VideoInformation>.Empty);
            var videosList = find.ToList();

            foreach (var videoInformation in videosList)
            {
                if (videoInformation.Name.Contains("Footworki"))
                {
                    videoInformation.VideoOwner = new VideoOwner()
                    {
                        OwnerId = Constants.WarsztatyFootworki2022,
                        OwnerType = OwnerType.Event
                    };
                } else if (videoInformation.Name.Contains("Rama"))
                {
                    videoInformation.VideoOwner = new VideoOwner()
                    {
                        OwnerId = Constants.WarsztatyRama2022,
                        OwnerType = OwnerType.Event
                    };
                }
                else
                {
                    videoInformation.VideoOwner = new VideoOwner()
                    {
                        OwnerId = Constants.GroupSroda1730,
                        OwnerType = OwnerType.Group
                    };
                }
            
                await videos.ReplaceOneAsync((s) => s.Id == videoInformation.Id, videoInformation);
            }
        }

        private static async Task CreateOwnersAsync()
        {
            var attenders = new List<string>() { TomekUserId };
            
            var events = app.Services.GetRequiredService<IMongoCollection<Event>>();
            await events.InsertManyAsync(new []
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

        private static Task SetUsersAccounts(IUserService userService)
        {
            var address = new
            {
                street_address = "Somwhere",
                locality = "In",
                postal_code = 1337,
                country = "Poland"
            };

            return userService.AddUpsertUserAsync(new Services.Models.UserModel()
            {

                SubjectId = "818727",
                Username = "alice",
                Password = "alice",
                Claims =
                        {
                            new Claim(JwtClaimTypes.Name, "Alice Smith"),
                            new Claim(JwtClaimTypes.GivenName, "Alice"),
                            new Claim(JwtClaimTypes.FamilyName, "Smith"),
                            new Claim(JwtClaimTypes.Email, "AliceSmith@email.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                            new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
                            new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json)
                        }

            });
        }

        public static async Task SetBasicIdentityConfiguration(IdentityResourceMongoStore resources, IdentityClientMongoStore clientStore)
        {

            Console.WriteLine("Are you sure to update configuration? This may override current settings and break application. y/N");

            var input = Console.ReadLine();
            if (input?.Equals("y", StringComparison.InvariantCultureIgnoreCase) == true)
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
