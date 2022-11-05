using IdentityServer4;
using IdentityServer4.Models;
using MongoDB.Driver;
using TB.DanceDance.Configurations;
using TB.DanceDance.Core;
using TB.DanceDance.Core.IdentityServerStore;
using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.MongoDb;
using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Services;
using TB.DanceDance.Services.Models;

var builder = WebApplication.CreateBuilder(args);

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
    o.AddPolicy(Config.ReadScope, c =>
    {
        c.AddAuthenticationSchemes(IdentityServerConstants.LocalApi.AuthenticationScheme);
        c.RequireAuthenticatedUser();
    });
});

builder.Services
    .ConfigureDb()
    .ConfigureVideoServices();

builder.Services
    .AddAuthentication()
    .AddLocalApi(o =>
{
    o.ExpectedScope = Config.ReadScope;
});


// Configuration of IdentityServer4
var identityBuilder = builder.Services
    .AddIdentityServer();

var setIdentityServerAsProduction = builder.Environment.IsProduction();

if (setIdentityServerAsProduction)
{
    builder.Services.AddScoped<IUserService, UserService>();
    identityBuilder
        .AddClientStore<IdentityClientMongoStore>()
        .AddResourceStore<IdentityResourceMongoStore>();
}
else
{
    builder.Services
        .AddSingleton<IUserService, TestUsersService>();

    identityBuilder
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

app.UseIdentityServer();
app.UseAuthorization();
app.MapControllers();

app.Run();



