using AspNetCore.Identity.MongoDbCore.Extensions;
using AspNetCore.Identity.MongoDbCore.Infrastructure;
using IdentityServer4;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography.X509Certificates;
using TB.DanceDance.Configurations;
using TB.DanceDance.Core;
using TB.DanceDance.Core.IdentityServerStore;
using TB.DanceDance.Identity;
using TB.DanceDance.Identity.IdentityResources;
using TB.DanceDance.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true);

// Add services to the container.

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
    .ConfigureDb(builder.Configuration.GetMongoDbConfig())
    .ConfigureVideoServices(ConnectionStringProvider.GetBlobConnectionString(builder.Configuration));

builder.Services
    .AddAuthentication()
    .AddLocalApi(o =>
{
    o.ExpectedScope = DanceDanceResources.WestCoastSwing.Scopes.ReadScope;
});

// Configuring dotnetIdentity and mongodb as storage
IdentityBuilder dotnetIdentityBuilder = builder.Services.AddIdentity<UserModel, RoleModel>();

string dotnetIdentityMongoConnectionString = ConnectionStringProvider.GetMongoDbConnectionStringForIdentityStore(builder.Configuration);
string dotnetIdentityMongoDatabase = "IdentityStore";

if (builder.Environment.IsProduction())
{
    dotnetIdentityBuilder.AddMongoDbStores<UserModel, RoleModel, Guid>(
        dotnetIdentityMongoConnectionString,
        dotnetIdentityMongoDatabase
    );
}
else
{
    var mongoDbIdentityConfiguration = new MongoDbIdentityConfiguration
    {
        MongoDbSettings = new MongoDbSettings
        {
            ConnectionString = dotnetIdentityMongoConnectionString,
            DatabaseName = dotnetIdentityMongoDatabase
        },
        IdentityOptionsAction = options =>
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
        }
    };
    builder.Services.ConfigureMongoDbIdentity<UserModel, RoleModel, Guid>(mongoDbIdentityConfiguration);
}


// Configuration of IdentityServer4
var identityBuilder = builder.Services
    .AddIdentityServer();

var setIdentityServerAsProduction = builder.Environment.IsProduction();


if (setIdentityServerAsProduction)
{
    builder.Services.AddScoped<IUserService, UserService>();

    var cert = Environment.GetEnvironmentVariable("TB.DanceDance.IdpCert");
    if (cert == null)
        throw new Exception("Cert is not available in environment variables");

    var password = Environment.GetEnvironmentVariable("TB.DanceDance.IdpCertPassword");
    var certBytes = Convert.FromBase64String(cert);

    identityBuilder
        .AddClientStore<IdentityClientMongoStore>()
        .AddResourceStore<IdentityResourceMongoStore>()
        .AddSigningCredential(new X509Certificate2(certBytes, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet));
}
else
{
    builder.Services
        .AddSingleton<IUserService, TestUsersService>();

    identityBuilder
        .AddAspNetIdentity<UserModel>()
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



