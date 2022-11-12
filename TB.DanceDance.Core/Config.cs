using Duende.IdentityServer.Models;
using TB.DanceDance.Core.IdentityServerStore;

namespace TB.DanceDance.Core
{
    public class Config
    {

        public const string ReadScope = "tbdancedanceapi.read";
        public const string WriteScope = "tbdancedanceapi.write";

        public static IEnumerable<ApiScopeRecord> ApiScopes =>
           new List<ApiScopeRecord>
           {
               new ("tbdancedanceapi.read", "TB DanceDance API - read"),
               new ("tbdancedanceapi.write", "TB DanceDance API - write")
           };

        public static IEnumerable<ApiResourceRecord> ApiResources =>
           new List<ApiResourceRecord>
           {
                new ("tbdancedanceapi") {
                Scopes = {"read", "write" },
                DisplayName = "TB DanceDance API",
                ShowInDiscoveryDocument = true,
                }
           };

        private static T ConvertTo<TFrom, T>(TFrom from)
        {
            // todo eliminate this workaround
            // due to mongo db deserialization, id is required
            // that is why I am using a DTO object with id property
            // id should be configured on mongo collection without modification of the class
            var serialized = System.Text.Json.JsonSerializer.Serialize(from);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<T>(serialized);
            if (deserialized == null)
                throw new Exception("Something went wrong on deserialization");
            return deserialized;
        }

        public static IEnumerable<IdentityResourceRecord> GetIdentityResources()
        {
            return new List<IdentityResourceRecord>
                {
                    ConvertTo<IdentityResource, IdentityResourceRecord>(new IdentityResources.OpenId()),
                    ConvertTo<IdentityResource, IdentityResourceRecord>(new IdentityResources.Profile()),
                    ConvertTo<IdentityResource, IdentityResourceRecord>(new IdentityResources.Email())
                };
        }

        public static IEnumerable<ClientRecord> Clients =>
            new List<ClientRecord>
            {
                new()
                {
                    ClientId = "tbdancedancefront",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,

                    // secret for authentication
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },
                    RequireClientSecret = false,

                    RedirectUris = { "http://localhost:3000/callback", "http://localhost:3000/" },

                    AllowedCorsOrigins =
                    {
                        "http://localhost:3000",
                        "https://localhost:3000"
                    },

                    // scopes that client has access to
                    AllowedScopes = { "tbdancedanceapi.read", "openid", "profile" }
                }
            };
    }
}
