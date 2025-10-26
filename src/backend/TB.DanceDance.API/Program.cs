using Application;
using Domain.Exceptions;
using IdentityServer4;
using Infrastructure;
using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authorization;
using TB.DanceDance.API;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true);
}

// Add services to the container.

builder.Services.RegisterApplicationServices();
builder.Services.RegisterInfrastructureServices(builder.Configuration, builder.Environment.IsProduction());

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

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(DanceDanceResources.WestCoastSwing.Scopes.ReadScope, c =>
    {
        c.AddAuthenticationSchemes(IdentityServerConstants.LocalApi.AuthenticationScheme);
        c.RequireAuthenticatedUser();
    });

    o.AddPolicy(DanceDanceResources.WestCoastSwing.Scopes.WriteConvert, c =>
    {
        c.RequireScope(DanceDanceResources.WestCoastSwing.Scopes.WriteConvert);
        c.RequireAuthenticatedUser();
    });

});

var authBuilder = builder.Services
    .AddAuthentication()
    .AddLocalApi(o =>
    {
        o.ExpectedScope = DanceDanceResources.WestCoastSwing.Scopes.ReadScope;
    });

var configureGoogleAuth = builder.Configuration["Authentication:Google:ClientId"] != null;

if (configureGoogleAuth)
{
    authBuilder.AddGoogle(googleOptions =>
     {
         googleOptions.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

         googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new AppException("Google Client Id is null.");
         googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? throw new AppException("Google Client Secret is null.");
     });
}

builder.Services.AddScoped<IIdentityClient, IdentityClient>();

var app = builder.Build();
app.UseCors();
#if DEBUG
// Enable http for debug
#else
app.UseHttpsRedirection();
#endif


app.UseStaticFiles();
app.UseIdentityServer();
app.UseAuthorization();
app.MapControllers();

app.Run();



