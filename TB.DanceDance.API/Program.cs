using IdentityServer4.Models;
using IdentityServerHost.Quickstart.UI;
using TB.DanceDance.API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddCors(setup =>
{
    setup.AddDefaultPolicy(c =>
    {
        // Todo why this does not work?
        //c.WithOrigins("http://localhost:3000/")
        //        .AllowAnyHeader()
        //        .AllowAnyMethod()
        //        .SetIsOriginAllowedToAllowWildcardSubdomains();
        c.AllowAnyHeader();
        c.AllowAnyMethod();
        c.AllowAnyOrigin();

    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddIdentityServer()
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
app.MapControllers();

app.Run();
