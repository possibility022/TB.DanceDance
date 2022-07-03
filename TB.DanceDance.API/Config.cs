using IdentityServer4.Models;

namespace TB.DanceDance.API
{
    public class Config
    {
        public static IEnumerable<ApiScope> ApiScopes =>
           new List<ApiScope>
           {
               new ApiScope("tbdancedanceapi.read", "TB DanceDance API - read")
           };

        public static IEnumerable<ApiResource> ApiResources =>
           new List<ApiResource>
           {
                new ApiResource("tbdancedanceapi") {
                Scopes = {"read", "write" },
                DisplayName = "TB DanceDance API",
                ShowInDiscoveryDocument = true,
                

                }
           };

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
                {
                    new IdentityResources.OpenId(),
                    new IdentityResources.Profile(),
                    new IdentityResources.Email(),
                };
        }

        public static IEnumerable<Client> Clients =>
            new List<Client>
            {
                new Client
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

                    RedirectUris = { "http://localhost:3000/callback.html", "http://localhost:3000/" },
                    
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
