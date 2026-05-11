using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TB.Auth.Web;
using TB.Auth.Web.Endpoints;
using TB.Auth.Web.Identity;

var builder = WebApplication.CreateBuilder(args);

OtelConfiguration.ConfigureOpenTelemetryAndLogging(builder);

// appsettings.json and appsettings.{Environment}.json are already loaded by CreateBuilder
// with correct priority (below env vars). Only add non-standard files here.
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);

if (builder.Environment.IsEnvironment("QA"))
    builder.Configuration.AddJsonFile("appsettings.QA.json", optional: true);

var authOptions = builder.Configuration.GetSection(AuthServerOptions.SectionName).Get<AuthServerOptions>() ?? new AuthServerOptions();
builder.Services.AddSingleton(authOptions);
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

if (authOptions.AllowWeakPasswords)
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

if (authOptions.AllowWeakPasswords)
{
    Console.WriteLine("WARNING - Weak Passwords are enabled!");
    app.MapDevelopmentEndpoints();
}
app.MapEndpoints(googleEnabled, authOptions.AllowWeakPasswords);

await app.RunAsync();


