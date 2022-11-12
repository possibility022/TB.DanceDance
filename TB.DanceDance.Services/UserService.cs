using IdentityModel;
using MongoDB.Driver;
using System.Security.Claims;
using System.Text.Json;
using Duende.IdentityServer;
using TB.DanceDance.Services.Models;

namespace TB.DanceDance.Services
{
    public class UserService : IUserService
    {
        private readonly IMongoCollection<UserModel> usersCollection;

        public UserService(IMongoCollection<UserModel> usersCollection)
        {
            this.usersCollection = usersCollection;
        }

        public async Task<UserModel> FindUserByNameAsync(string name)
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

        public Task<UserModel> FindUserByNameAsync(string name)
        {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
            return Task.FromResult(users.Find(f => f.Username == name));
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
        }

        public bool ValidateCredentials(string username, string password)
        {
            return users.Any(f => f.Username == username && f.Password == password);
        }
    }
}
