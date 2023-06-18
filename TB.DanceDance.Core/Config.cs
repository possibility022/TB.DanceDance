using IdentityServer4.Models;
using TB.DanceDance.Identity.IdentityResources;

namespace TB.DanceDance.Core;

public class Config
{
    public static IEnumerable<ApiScope> ApiScopes =>
       new List<ApiScope>
       {
           new (DanceDanceResources.WestCoastSwing.Scopes.ReadScope, "TB DanceDance API - read"),
           new (DanceDanceResources.WestCoastSwing.Scopes.WriteScope, "TB DanceDance API - write"),
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

                AllowedCorsOrigins =
                {
                    "http://localhost:3000",
                    "https://localhost:3000"
                },

                // scopes that client has access to
                AllowedScopes = { DanceDanceResources.WestCoastSwing.Scopes.ReadScope, "openid", "profile" }
            }
        };
}
