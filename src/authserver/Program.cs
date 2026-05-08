using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TB.Auth.Web;
using TB.Auth.Web.Endpoints;
using TB.Auth.Web.Identity;

var builder = WebApplication.CreateBuilder(args);

OtelConfiguration.ConfigureOpenTelemetryAndLogging(builder);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true);
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);
}


var authOptions = builder.Configuration.GetSection(AuthServerOptions.SectionName).Get<AuthServerOptions>() ?? new AuthServerOptions();
if (authOptions.AllowedCorsOrigins.Length == 0)
{
    authOptions.AllowedCorsOrigins =
    [
        "http://localhost:3000",
        "http://localhost:4200",
        "http://localhost:5112"
    ];
}

var identityConnectionString = builder.Configuration.GetConnectionString("AuthDbConnectionString")
                                ?? throw new InvalidOperationException("Connection string 'AuthDbConnectionString' is required.");

builder.Services.AddDbContext<IdentityStoreContext>(options =>
{
    options.UseNpgsql(identityConnectionString);
});

builder.Services.AddDbContext<AuthStoreContext>(options =>
{
    options.UseNpgsql(identityConnectionString);
    options.UseOpenIddict();
});

builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<IdentityStoreContext>()
    .AddDefaultTokenProviders();

if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<IdentityOptions>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 3;
        options.Password.RequiredUniqueChars = 1;
    });
}



var openIddictBuilder = builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<AuthStoreContext>();
    });

var googleEnabled = openIddictBuilder.AddGoogleClient(builder, authOptions);
openIddictBuilder.AddServerWithConfiguration(authOptions, builder.Environment.IsDevelopment());

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(authOptions.AllowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapDevelopmentEndpoints();
}
app.MapEndpoints(googleEnabled);

await using (var scope = app.Services.CreateAsyncScope())
{
    //await scope.ServiceProvider.InitializeDevDataAsync();
}

await app.RunAsync();


