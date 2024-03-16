using IdentityServer4.Models;
using Infrastructure.Identity.IdentityResources;

namespace Infrastructure;

class Config
{
    public static IEnumerable<ApiScope> ApiScopes =>
       new List<ApiScope>
       {
           new (DanceDanceResources.WestCoastSwing.Scopes.ReadScope, "TB DanceDance API - read"),
           new (DanceDanceResources.WestCoastSwing.Scopes.WriteScope, "TB DanceDance API - write"),
           new (DanceDanceResources.WestCoastSwing.Scopes.WriteConvert, "TB DanceDance API - converter"),
       };

    public static IEnumerable<ApiResource> ApiResources =>
       new List<ApiResource>
       {
            new ("tbdancedanceapi") {
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
                new IdentityResources.Email()
            };
    }

    public static IEnumerable<Client> Clients =>
        new List<Client>
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
                AllowOfflineAccess = true,
                AllowedCorsOrigins =
                {
                    "http://localhost:3000",
                    "https://localhost:3000"
                },

                // scopes that client has access to
                AllowedScopes = { DanceDanceResources.WestCoastSwing.Scopes.ReadScope, "openid", "profile" }
            },
            new()
            {
                ClientId = "tbdancedanceconverter",
                ClientName = "Converter Service",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                RequireClientSecret = true,

                ClientSecrets =
                {
                    new Secret("other".Sha256())
                },
                AllowedScopes =
                {
                    DanceDanceResources.WestCoastSwing.Scopes.WriteConvert
                }
            }
        };
}
