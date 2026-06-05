using System.Security.Cryptography.X509Certificates;
using OpenIddict.Abstractions;
using TB.DanceDance.API.Contracts.ApiResources;
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
}
