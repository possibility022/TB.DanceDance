using IdentityServer4;
using IdentityServerHost.Quickstart.UI;
using MongoDB.Driver;
using TB.DanceDance.API;
using TB.DanceDance.Configurations;
using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.MongoDb;
using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddCors(setup =>
{
    setup.AddDefaultPolicy(c =>
    {
        // Todo why this does not work?
        c.WithOrigins("http://localhost:3000/", "http://localhost:3000", "https://localhost:3000/", "https://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowedToAllowWildcardSubdomains();
        //c.AllowAnyHeader();
        //c.AllowAnyMethod();
        //c.AllowAnyOrigin();

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


var config = new MongoDbConfiguration();
builder.Services
    .AddSingleton<IMongoClient>((services) =>
    {
        return MongoDatabaseFactory.GetClient();
    })
    .AddSingleton<IMongoDatabase>((services) =>
    {
        var mongoClient = services.GetRequiredService<IMongoClient>();
        var db = mongoClient.GetDatabase(config.Database);
        return db;
    })
    .AddSingleton<IMongoCollection<VideoInformation>>(services =>
    {
        var db = services.GetRequiredService<IMongoDatabase>();
        var collection = db.GetCollection<VideoInformation>(config.VideoCollection);
        return collection;
    });

var blobConfig = new BlobConfiguration();

builder.Services
    .AddSingleton<IBlobDataService>(new BlobDataService(ApplicationBlobContainerFactory.TryGetConnectionStringFromEnvironmentVariables(), blobConfig.BlobContainer))
    .AddScoped<IVideoService, VideoService>()
    .AddScoped<IVideoFileLoader, FakeFileLoader>();

builder.Services
    .AddAuthentication()
    .AddLocalApi(o =>
{
    o.ExpectedScope = Config.ReadScope;
});

// Configuration of IdentityServer4
builder.Services
    .AddIdentityServer()
    .AddDeveloperSigningCredential()
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryClients(Config.Clients)
    .AddInMemoryApiResources(Config.ApiResources)
    .AddInMemoryIdentityResources(Config.GetIdentityResources())
    .AddTestUsers(TestUsers.Users);





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
