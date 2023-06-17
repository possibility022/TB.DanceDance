using IdentityServer4;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using TB.DanceDance.Configurations;
using TB.DanceDance.Core;
using TB.DanceDance.Data.PostgreSQL;
using TB.DanceDance.Identity;
using TB.DanceDance.Identity.IdentityResources;
using TB.DanceDance.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true);
}

// Add services to the container.

builder.Services.AddDbContext<DanceDbContext>(options =>
{
    options.UseNpgsql(ConnectionStringProvider.GetPostgreSqlDbConnectionString(builder.Configuration));
});

builder.Services.AddDbContext<IdentityStoreContext>(options =>
{
    options.UseNpgsql(ConnectionStringProvider.GetPostgreIdentityStoreDbConnectionString(builder.Configuration));
});

builder.Services.AddControllersWithViews();
builder.Services.AddCors(setup =>
{
    setup.AddDefaultPolicy(c =>
    {
        if (builder.Environment.IsDevelopment())
        {
            c.WithOrigins(CorsConfig.GetDevOrigins());
        }
        else
        {
            var config = CorsConfig.GetFromEnvironmentVariable();
            c.WithOrigins(config.AllowedOrigins);
        }

        c.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowedToAllowWildcardSubdomains();

    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

// This line enables build in authentication by IdentityServer4
// http://docs.identityserver.io/en/latest/topics/add_apis.html

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(DanceDanceResources.WestCoastSwing.Scopes.ReadScope, c =>
    {
        c.AddAuthenticationSchemes(IdentityServerConstants.LocalApi.AuthenticationScheme);
        c.RequireAuthenticatedUser();
    });
});

builder.Services
    .ConfigureVideoServices(ConnectionStringProvider.GetBlobConnectionString(builder.Configuration));

builder.Services
    .AddAuthentication()
    .AddLocalApi(o =>
{
    o.ExpectedScope = DanceDanceResources.WestCoastSwing.Scopes.ReadScope;
});


builder.Services
    .AddIdentity<User, Role>()
    .AddEntityFrameworkStores<IdentityStoreContext>();

if (builder.Environment.IsProduction())
{
    // Default configuration for IdentityOptions is fine.
}
else
{
    builder.Services.Configure<IdentityOptions>(options =>
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
var identityBuilder = builder.Services
    .AddIdentityServer();

var setIdentityServerAsProduction = builder.Environment.IsProduction();

builder.Services.AddScoped<IUserService, UserService>();
var migrationsAssembly = TB.DanceDance.Identity.DesignTimeContextFactory.GetMigrationAssembly();

if (setIdentityServerAsProduction)
{
    var cert = Environment.GetEnvironmentVariable("TB.DanceDance.IdpCert");
    if (cert == null)
        throw new Exception("Cert is not available in environment variables");

    var password = Environment.GetEnvironmentVariable("TB.DanceDance.IdpCertPassword");
    var certBytes = Convert.FromBase64String(cert);

    identityBuilder
        .AddAspNetIdentity<User>()
        .AddDeveloperSigningCredential()
        .AddConfigurationStore(options =>
        {
            options.ConfigureDbContext = b =>
            {
                b.UseNpgsql(ConnectionStringProvider.GetPostgreIdentityStoreDbConnectionString(builder.Configuration));
            };
        })
        .AddOperationalStore(options =>
        {
            options.ConfigureDbContext = b =>
            {
                b.UseNpgsql(ConnectionStringProvider.GetPostgreIdentityStoreDbConnectionString(builder.Configuration));
            };
        }) 
        .AddSigningCredential(new X509Certificate2(certBytes, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet));
}
else
{
    identityBuilder
        .AddAspNetIdentity<User>()
        .AddDeveloperSigningCredential()
        .AddInMemoryApiScopes(Config.ApiScopes)
        .AddInMemoryClients(Config.Clients)
        .AddInMemoryApiResources(Config.ApiResources)
        .AddInMemoryIdentityResources(Config.GetIdentityResources());
}

var app = builder.Build();
app.UseCors();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();


app.UseStaticFiles();
app.UseIdentityServer();
app.UseAuthorization();
app.MapControllers();

app.Run();



