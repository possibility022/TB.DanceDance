using System.Security.Cryptography.X509Certificates;
using Infrastructure.Identity.IdentityResources;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TB.Auth.Web;

public static class Configuration
{
    extension(OpenIddictBuilder builder)
    {
        public void AddServerWithConfiguration(AuthServerOptions authOptions, bool isDevelopment)
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
                    DanceDanceResources.WestCoastSwing.Scopes.ReadScope,
                    DanceDanceResources.WestCoastSwing.Scopes.WriteConvert);

                options.AllowAuthorizationCodeFlow()
                    .AllowRefreshTokenFlow()
                    .AllowClientCredentialsFlow();

                var signingCert = LoadCertIfPresent(
                    authOptions.ServerSigningCertificateBase64,
                    authOptions.ServerSigningCertificatePassword);
                var encryptionCert = LoadCertIfPresent(
                    authOptions.ServerEncryptionCertificateBase64,
                    authOptions.ServerEncryptionCertificatePassword);

                if (signingCert is not null && encryptionCert is not null)
                {
                    options.AddSigningCertificate(signingCert)
                        .AddEncryptionCertificate(encryptionCert);
                }
                else if (isDevelopment && signingCert is null && encryptionCert is null)
                {
                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();
                }
                else
                {
                    throw new InvalidOperationException(
                        "Server certificates are misconfigured. Configure both AuthServer:ServerSigningCertificate* and AuthServer:ServerEncryptionCertificate*.");
                }

                options.DisableAccessTokenEncryption();

                options.SetIssuer(new Uri(authOptions.Issuer));

                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .EnableTokenEndpointPassthrough();
            });
        }

        public bool AddGoogleClient(WebApplicationBuilder webApplicationBuilder, AuthServerOptions authOptions)
        {
            var googleClientId = webApplicationBuilder.Configuration["Authentication:Google:ClientId"];
            var googleClientSecret = webApplicationBuilder.Configuration["Authentication:Google:ClientSecret"];
            var googleEnabled = !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret);

            if (googleEnabled is false)
                return false;

            builder.AddClient(options =>
            {
                options.AllowAuthorizationCodeFlow();
                options.SetRedirectionEndpointUris("callback/login/google");

                var signingCert = LoadCertIfPresent(
                    authOptions.ClientSigningCertificateBase64,
                    authOptions.ClientSigningCertificatePassword);
                var encryptionCert = LoadCertIfPresent(
                    authOptions.ClientEncryptionCertificateBase64,
                    authOptions.ClientEncryptionCertificatePassword);

                if (signingCert is not null && encryptionCert is not null)
                {
                    options.AddSigningCertificate(signingCert)
                        .AddEncryptionCertificate(encryptionCert);
                }
                else if (webApplicationBuilder.Environment.IsDevelopment() && signingCert is null && encryptionCert is null)
                {
                    options.AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }
                else
                {
                    throw new InvalidOperationException(
                        "Client certificates are misconfigured. Configure both AuthServer:ClientSigningCertificate* and AuthServer:ClientEncryptionCertificate*.");
                }

                options.UseAspNetCore()
                    .EnableRedirectionEndpointPassthrough();

                options.UseSystemNetHttp()
                    .SetProductInformation(typeof(Program).Assembly);

                options.UseWebProviders()
                    .AddGoogle(google =>
                    {
                        google.SetClientId(googleClientId!);
                        google.SetClientSecret(googleClientSecret!);
                        google.SetRedirectUri("callback/login/google");
                    });
            });

            return true;
        }
        
        private static X509Certificate2? LoadCertIfPresent(string? certAsBase64, string? certPassword)
        {
            var cert = certAsBase64;
            var password = certPassword;

            if (string.IsNullOrEmpty(cert) || string.IsNullOrEmpty(password))
                return null;
            
            var certBytes = Convert.FromBase64String(cert);
            var signedCert = X509CertificateLoader.LoadPkcs12(certBytes, password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);

            return signedCert;
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
                    Name = DanceDanceResources.WestCoastSwing.Scopes.ReadScope,
                    DisplayName = "TB DanceDance API - read",
                    Resources = { "tbdancedanceapi" }
                });

            await UpsertScope(
                scopeManager,
                new OpenIddictScopeDescriptor
                {
                    Name = DanceDanceResources.WestCoastSwing.Scopes.WriteConvert,
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
                DanceDanceResources.WestCoastSwing.Scopes.ReadScope);

            await UpsertApplication(applicationManager, frontClientDescriptor);

            var converterClientDescriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "tbdancedanceconverter",
                ClientType = ClientTypes.Confidential,
                ClientSecret = "other",
                DisplayName = "TB DanceDance Converter Daemon",
                Permissions = { Permissions.Endpoints.Token, Permissions.GrantTypes.ClientCredentials }
            };

            converterClientDescriptor.AddScopePermissions(DanceDanceResources.WestCoastSwing.Scopes.WriteConvert);

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
                DanceDanceResources.WestCoastSwing.Scopes.ReadScope);

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
