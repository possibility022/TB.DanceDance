using Application;
using Application.Services;
using Domain;
using Domain.Exceptions;
using Domain.Services;
using Infrastructure.Data;
using Infrastructure.Data.BlobStorage;
using Infrastructure.Identity;
using Infrastructure.Identity.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;

namespace Infrastructure;

public static class DependencyInjection
{
    private static X509Certificate2? LoadCertIfPresent(IConfiguration configuration)
    {
        var cert = configuration.GetSection("TB").GetSection("DanceDance")["IdpCert"];
        var password = configuration.GetSection("TB").GetSection("DanceDance")["IdpCertPassword"];

        if (string.IsNullOrEmpty(cert) || string.IsNullOrEmpty(password))
            return null;
            
        var certBytes = Convert.FromBase64String(cert);
        var signedCert = X509CertificateLoader.LoadPkcs12(certBytes, password,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);

        return signedCert;
    }
    
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterInfrastructureServices(IConfiguration configuration,
            bool productionPolicies)
        {

#pragma warning disable CS8604 // Possible null reference argument.
            services.AddSingleton<IBlobDataServiceFactory>(r =>
                new BlobDataServiceFactory(configuration.GetConnectionString("Blob")));
#pragma warning restore CS8604 // Possible null reference argument.

            services.AddDbContext<DanceDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("PostgreDb" ??
                                                                    throw new AppException(
                                                                        "PostgreDb connection string is null.")));
            });

            services.AddDbContext<IdentityStoreContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("PostgreDbIdentityStore") ??
                                  throw new AppException("PostgreDbIdentityStore connection string is null."));
            });

            services.AddScoped<IApplicationContext>(provider => provider.GetRequiredService<DanceDbContext>());

            services
                .AddIdentity<User, Role>()
                .AddEntityFrameworkStores<IdentityStoreContext>();

            if (productionPolicies)
            {
                // Default configuration for IdentityOptions is fine.
            }
            else
            {
                services.Configure<IdentityOptions>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequiredLength = 4;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;

                    // Lockout settings
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                    options.Lockout.MaxFailedAccessAttempts = 10;

                    // ApplicationUser settings
                    options.User.RequireUniqueEmail = true;
                });
            }

            // Configuration of IdentityServer4
            var identityBuilder = services
                .AddIdentityServer();


            services.AddScoped<IAccessManagementService, AccessManagementService>();

            if (productionPolicies)
            {
                var signedCert = LoadCertIfPresent(configuration);
                if (signedCert is null)
                    throw new AppException("Cert is not available in configuration. It is required for production setup.");

                identityBuilder
                    .AddAspNetIdentity<User>()
                    .RegisterIdentityServerStorage(configuration.GetConnectionString("PostgreDbIdentityStore") ??
                                                   throw new AppException("Identity connection string is null."))
                    .AddSigningCredential(signedCert)
                    .AddProfileService<TbProfileService>();
            }
            else
            {
                var signedCert = LoadCertIfPresent(configuration);
                
                identityBuilder
                    .AddAspNetIdentity<User>();

                if (signedCert is null)
                    identityBuilder.AddDeveloperSigningCredential();
                else
                    identityBuilder.AddSigningCredential(signedCert);
                    
                identityBuilder
                    .RegisterIdentityServerStorage(configuration.GetConnectionString("PostgreDbIdentityStore") ??
                                                   throw new AppException("Identity connection string is null."))
                    // for debugging
                    //.AddInMemoryApiScopes(Config.ApiScopes)
                    //.AddInMemoryClients(Config.Clients)
                    //.AddInMemoryApiResources(Config.ApiResources)
                    //.AddInMemoryIdentityResources(Config.GetIdentityResources())
                    .AddProfileService<TbProfileService>();
            }

            return services;
        }
    }
}
