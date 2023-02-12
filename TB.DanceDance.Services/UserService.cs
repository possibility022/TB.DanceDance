using IdentityModel;
using IdentityServer4;
using MongoDB.Driver;
using System.Security.Claims;
using System.Text.Json;
using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Services.Models;

namespace TB.DanceDance.Services
{
    public class UserService : IUserService
    {
        private readonly IMongoCollection<UserModel> usersCollection;
        private readonly IMongoCollection<Event> events;
        private readonly IMongoCollection<Group> groups;

        public UserService(IMongoCollection<UserModel> usersCollection
        , IMongoCollection<Event> events
        , IMongoCollection<Group> groups)
        {
            this.usersCollection = usersCollection;
            this.events = events;
            this.groups = groups;
        }

        public async Task<UserModel?> FindUserByNameAsync(string name)
        {
            var filter = new FilterDefinitionBuilder<UserModel>()
                .Eq(r => r.Username, name);

            var res = await usersCollection.FindAsync(filter);
            return await res.FirstAsync();
        }

        public bool ValidateCredentials(string username, string password)
        {
            // todo migrate to asp net identity
            return usersCollection
                .Find(f => f.Username == username && f.Password == password)
                .Any();
        }

        public Task AddUpsertUserAsync(UserModel model)
        {
            return usersCollection.ReplaceOneAsync(f => f.SubjectId == model.SubjectId, model, new ReplaceOptions()
            {
                IsUpsert = true
            });
        }

        public async Task<(ICollection<Group>, ICollection<Event>)> GetUserEventsAndGroups(string userName)
        {
            if (userName is null)
                throw new ArgumentNullException(nameof(userName));

            List<Group> userGroups = null!;
            List<Event> userEvents = null!;

            var getGroups = this.groups.FindAsync(r => r.People.Contains(userName))
                    .ContinueWith(async r => userGroups = await r.Result.ToListAsync());

            var getEvents = this.events.FindAsync(r => r.Attenders.Contains(userName))
                .ContinueWith(async r => userEvents = await r.Result.ToListAsync());

            await Task.WhenAll(getGroups, getEvents);

            return (userGroups, userEvents);
        }

        public async Task<IEnumerable<string>> GetUserVideosAssociationsIds(string userName)
        {
            (var userGroups, var userEvents) = await GetUserEventsAndGroups(userName);

            return userGroups.Select(r => r.Id)
                .Concat(userEvents.Select(e => e.Id));
        }

        public async Task<bool> UserIsAssociatedWith(string userName, string entityId)
        {
            var associatedTo = await GetUserVideosAssociationsIds(userName);
            return associatedTo.Any(r => r == entityId);
        }
    }

    public class TestUsersService : IUserService
    {
        private List<UserModel> users;

        public TestUsersService()
        {
            var address = new
            {
                street_address = "One Hacker Way",
                locality = "Heidelberg",
                postal_code = 69118,
                country = "Germany"
            };

            users = new List<UserModel>
                {
                    new UserModel
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
                    },
                    new UserModel
                    {
                        SubjectId = "88421113",
                        Username = "bob",
                        Password = "bob",
                        Claims =
                        {
                            new Claim(JwtClaimTypes.Name, "Bob Smith"),
                            new Claim(JwtClaimTypes.GivenName, "Bob"),
                            new Claim(JwtClaimTypes.FamilyName, "Smith"),
                            new Claim(JwtClaimTypes.Email, "BobSmith@email.com"),
                            new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                            new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                            new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json)
                        }
                    }
                };
        }

        public Task AddUpsertUserAsync(UserModel model)
        {
            throw new NotSupportedException();
        }

        public Task<IEnumerable<string>> GetUserVideosAssociationsIds(string userName)
        {
            IEnumerable<string> enumerable = this.users.Select(r => r.SubjectId).Concat(new[]
                { Constants.GroupSroda1730, Constants.WarsztatyFootworki2022, Constants.WarsztatyRama2022 })!;

            return Task.FromResult(enumerable);
        }

        public Task<UserModel?> FindUserByNameAsync(string name)
        {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
            return Task.FromResult(users.Find(f => f.Username == name));
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
        }

        public bool ValidateCredentials(string username, string password)
        {
            return users.Any(f => f.Username == username && f.Password == password);
        }

        public async Task<bool> UserIsAssociatedWith(string userName, string entityId)
        {
            var associatedTo = await GetUserVideosAssociationsIds(userName);
            return associatedTo.Any(r => r == entityId);
        }

        public Task<(ICollection<Group>, ICollection<Event>)> GetUserEventsAndGroups(string userName)
        {
            var groups = new Group[] { new Group()
            {
                Id = Constants.GroupSroda1730,
                GroupName = "Sroda 1800",
                People = new string[] {userName}
            } };

            var events = new Event[] {
                new Event()
                {
                    Id = Constants.WarsztatyFootworki2022,
                    Attenders= new[] {userName},
                    Date = new DateTime(2022, 05, 05),
                    Name = "Warsztaty Footworki 2022"
                },
                new Event()
                {
                    Id = Constants.WarsztatyRama2022,
                    Attenders= new[] {userName},
                    Date = new DateTime(2022, 05, 05),
                    Name = "Warsztaty Rama 2022"
                }
            };

            return Task.FromResult<(ICollection<Group>, ICollection<Event>)>(new (groups, events));
        }
    }
}
