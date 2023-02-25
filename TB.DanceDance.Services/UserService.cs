using AspNetCore.Identity.MongoDbCore.Models;
using IdentityModel;
using MongoDB.Driver;
using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Identity;

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
                .Eq(r => r.UserName, name);

            var res = await usersCollection.FindAsync(filter);
            return await res.FirstAsync();
        }

        public bool ValidateCredentials(string username, string password)
        {
            // todo migrate to asp net identity
            return usersCollection
                .Find(f => f.UserName == username && f.PasswordHash == password)
                .Any();
        }

        public Task AddUpsertUserAsync(UserModel model)
        {
            return usersCollection.ReplaceOneAsync(f => f.Id == model.Id, model, new ReplaceOptions()
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
            users = new List<UserModel>
                {
                    new UserModel
                    {
                        Id = Guid.Parse("edaddb7a-6dfc-4e0b-bf15-ecec4ef75de1"),
                        UserName = "alice",
                        PasswordHash = "alice", //todo hash
                        Claims =
                        {
                            new MongoClaim
                            {
                                Type = JwtClaimTypes.Name,
                                Value = "Alice Smith"
                            },
                            new MongoClaim()
                            {
                                Type = JwtClaimTypes.GivenName,
                                Value = "Alice"
                            },
                            new MongoClaim () {
                                Type = JwtClaimTypes.FamilyName,
                                Value = "Smith"
                            },
                            new MongoClaim () { Type = JwtClaimTypes.Email, Value = "AliceSmith@email.com" },
                            new MongoClaim () { Type = JwtClaimTypes.EmailVerified, Value = "true" },
                            new MongoClaim () { Type = JwtClaimTypes.WebSite, Value = "http://alice.com" },
                        }
                    },

                    new UserModel
                    {
                        Id = Guid.Parse("f8db4ddd-5e50-4526-9281-392edd47c5c4"),
                        UserName = "bob",
                        PasswordHash = "bob", //todo hash
                        Claims =
                        {
                            new MongoClaim () { Type = JwtClaimTypes.Name, Value = "Bob Smith" },
                            new MongoClaim () { Type = JwtClaimTypes.GivenName, Value = "Bob" },
                            new MongoClaim () { Type = JwtClaimTypes.FamilyName, Value = "Smith" },
                            new MongoClaim () { Type = JwtClaimTypes.Email, Value = "BobSmith@email.com" },
                            new MongoClaim () { Type = JwtClaimTypes.EmailVerified, Value = "true" },
                            new MongoClaim () { Type = JwtClaimTypes.WebSite, Value = "http://bob.com" },
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
            IEnumerable<string> enumerable = this.users.Select(r => r.Id.ToString()).Concat(new[]
                { Constants.GroupSroda1730, Constants.WarsztatyFootworki2022, Constants.WarsztatyRama2022 })!;

            return Task.FromResult(enumerable);
        }

        public Task<UserModel?> FindUserByNameAsync(string name)
        {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
            return Task.FromResult(users.Find(f => f.UserName == name));
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
        }

        public bool ValidateCredentials(string username, string password)
        {
            return users.Any(f => f.UserName == username && f.PasswordHash == password);
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

            return Task.FromResult<(ICollection<Group>, ICollection<Event>)>(new(groups, events));
        }
    }
}
