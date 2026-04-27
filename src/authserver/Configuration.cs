using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TB.Auth.Web;

public static class Configuration
{
    extension(OpenIddictBuilder builder)
    {
        public void AddServerWithConfiguration(AuthServerOptions authOptions)
        {
            builder.AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("connect/authorize")
                    .SetEndSessionEndpointUris("connect/logout")
                    .SetTokenEndpointUris("connect/token");

                options.RegisterScopes(
                    Scopes.OpenId,
                    Scopes.Profile,
                    Scopes.Email,
                    Scopes.OfflineAccess,
                    "tbdancedanceapi.read",
                    "tbdancedanceapi.convert");

                options.AllowAuthorizationCodeFlow()
                    .AllowRefreshTokenFlow()
                    .AllowClientCredentialsFlow();

                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                options.DisableAccessTokenEncryption();

                options.SetIssuer(new Uri(authOptions.Issuer));

                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .EnableTokenEndpointPassthrough();
            });
        }
    }

    extension(IServiceProvider services)
    {
        public async Task InitializeDevDataAsync()
        {
            var scopeManager = services.GetRequiredService<IOpenIddictScopeManager>();
            var applicationManager = services.GetRequiredService<IOpenIddictApplicationManager>();

            await UpsertScope(
                scopeManager,
                new OpenIddictScopeDescriptor
                {
                    Name = "tbdancedanceapi.read",
                    DisplayName = "TB DanceDance API - read",
                    Resources = { "tbdancedanceapi" }
                });

            await UpsertScope(
                scopeManager,
                new OpenIddictScopeDescriptor
                {
                    Name = "tbdancedanceapi.convert",
                    DisplayName = "TB DanceDance API - converter",
                    Resources = { "tbdancedanceapi" }
                });

            var frontClientDescriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "tbdancedancefront",
                ClientType = ClientTypes.Public,
                RedirectUris =
                {
                    new Uri("http://localhost:3000/callback"),
                    new Uri("http://localhost:4200/callback"),
                    new Uri("http://localhost:5112/signin-callback.html"),
                    new Uri("http://localhost:5112/signin-silent-callback.html"),
                    new Uri("http://localhost:5112/index.html")
                },
                PostLogoutRedirectUris = { new Uri("http://localhost:3000") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.EndSession,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange }
            };

            frontClientDescriptor.AddScopePermissions(Scopes.OpenId, Scopes.Profile, Scopes.Email, Scopes.OfflineAccess,
                "tbdancedanceapi.read");

            await UpsertApplication(applicationManager, frontClientDescriptor);

            var converterClientDescriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "tbdancedanceconverter",
                ClientType = ClientTypes.Confidential,
                ClientSecret = "other",
                DisplayName = "TB DanceDance Converter Daemon",
                Permissions = { Permissions.Endpoints.Token, Permissions.GrantTypes.ClientCredentials }
            };

            converterClientDescriptor.AddScopePermissions("tbdancedanceapi.convert");

            await UpsertApplication(applicationManager, converterClientDescriptor);

            var androidClientDescriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "tbdancedanceandroidapp",
                ClientType = ClientTypes.Public,
                DisplayName = "TB DanceDance Android App",
                RedirectUris =
                {
                    new Uri("tbdancedanceandroidapp://")
                },
                PostLogoutRedirectUris =
                {
                    new Uri("tbdancedanceandroidapp://")
                },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.EndSession,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            };

            androidClientDescriptor.AddScopePermissions(
                Scopes.OpenId,
                Scopes.Profile,
                Scopes.Email,
                Scopes.OfflineAccess,
                "tbdancedanceapi.read");

            await UpsertApplication(applicationManager, androidClientDescriptor);
        }

        static async Task UpsertScope(IOpenIddictScopeManager scopeManager, OpenIddictScopeDescriptor descriptor)
        {
            if (string.IsNullOrWhiteSpace(descriptor.Name))
            {
                throw new InvalidOperationException("OpenIddict scope descriptor name cannot be empty.");
            }

            var existingScope = await scopeManager.FindByNameAsync(descriptor.Name);
            if (existingScope is null)
            {
                await scopeManager.CreateAsync(descriptor);
                return;
            }

            await scopeManager.PopulateAsync(descriptor, existingScope);
            await scopeManager.UpdateAsync(existingScope, descriptor);
        }

        static async Task UpsertApplication(IOpenIddictApplicationManager applicationManager,
            OpenIddictApplicationDescriptor descriptor)
        {
            if (string.IsNullOrWhiteSpace(descriptor.ClientId))
            {
                throw new InvalidOperationException("OpenIddict application client id cannot be empty.");
            }

            var existingApplication = await applicationManager.FindByClientIdAsync(descriptor.ClientId);
            if (existingApplication is null)
            {
                await applicationManager.CreateAsync(descriptor);
                return;
            }

            await applicationManager.PopulateAsync(descriptor, existingApplication);
            await applicationManager.UpdateAsync(existingApplication, descriptor);
        }
    }
}
