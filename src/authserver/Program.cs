using System.Net;
using System.Security.Claims;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using TB.Auth.Web;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Client.WebIntegration.OpenIddictClientWebIntegrationConstants;

var builder = WebApplication.CreateBuilder(args);

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

var identityConnectionString = builder.Configuration.GetConnectionString("PostgreDbIdentityStore")
                               ?? throw new InvalidOperationException("Connection string 'PostgreDbIdentityStore' is required.");
var authStoreConnectionString = builder.Configuration.GetConnectionString("PostgreDbAuthStore")
                                ?? throw new InvalidOperationException("Connection string 'PostgreDbAuthStore' is required.");

builder.Services.AddDbContext<IdentityStoreContext>(options =>
{
    options.UseNpgsql(identityConnectionString);
});

builder.Services.AddDbContext<AuthStoreContext>(options =>
{
    options.UseNpgsql(authStoreConnectionString);
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

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleEnabled = !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret);

var openIddictBuilder = builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<AuthStoreContext>();
    });

if (googleEnabled)
{
    openIddictBuilder.AddClient(options =>
    {
        options.AllowAuthorizationCodeFlow();
        options.SetRedirectionEndpointUris("callback/login/google");

        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
            .EnableRedirectionEndpointPassthrough();

        options.UseSystemNetHttp()
            .SetProductInformation(typeof(Program).Assembly);

        if (googleEnabled)
        {
            options.UseWebProviders()
                .AddGoogle(options =>
                {
                    options.SetClientId(googleClientId!);
                    options.SetClientSecret(googleClientSecret!);
                    options.SetRedirectUri("callback/login/google");
                });
        }
    });
}

openIddictBuilder.AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("connect/authorize")
            .SetEndSessionEndpointUris("connect/logout")
            .SetTokenEndpointUris("connect/token");

        options.RegisterScopes(
            Scopes.OpenId,
            Scopes.Profile,
            Scopes.Email,
            Scopes.OfflineAccess,
            "tbdancedanceapi.read",
            "tbdancedanceapi.convert");

        options.AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow()
            .AllowClientCredentialsFlow();

        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        options.DisableAccessTokenEncryption();

        options.SetIssuer(new Uri(authOptions.Issuer));

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough()
            .EnableTokenEndpointPassthrough();
    });

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

app.MapGet("policy/dancedanceapp", () => Results.Text("Privacy policy page is not implemented yet."));

if (app.Environment.IsDevelopment())
{
    app.MapGet("dev/login", (HttpContext context) =>
    {
        var returnUrl = GetValidatedReturnUrl(context, context.Request.Query["returnUrl"].ToString());
        var error = context.Request.Query["error"].ToString();
        var message = context.Request.Query["message"].ToString();

        return Results.Content(BuildDevLoginHtml(returnUrl, error, message), "text/html");
    });

    app.MapPost("dev/login", async (HttpContext context, UserManager<User> userManager) =>
    {
        if (!context.Request.HasFormContentType)
        {
            return Results.BadRequest("Expected form body.");
        }

        var form = await context.Request.ReadFormAsync();
        var login = form["login"].ToString().Trim();
        var password = form["password"].ToString();
        var returnUrl = GetValidatedReturnUrl(context, form["returnUrl"].ToString());

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            return Results.Redirect($"/dev/login?returnUrl={Uri.EscapeDataString(returnUrl)}&error={Uri.EscapeDataString("Login and password are required.")}");
        }

        var user = await userManager.FindByNameAsync(login);
        if (user is null && login.Contains('@'))
        {
            user = await userManager.FindByEmailAsync(login);
        }

        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            return Results.Redirect($"/dev/login?returnUrl={Uri.EscapeDataString(returnUrl)}&error={Uri.EscapeDataString("Invalid login or password.")}");
        }

        var userClaims = await userManager.GetClaimsAsync(user);
        var displayName = userClaims.FirstOrDefault(claim => claim.Type == ClaimTypes.Name)?.Value ??
                          user.UserName ??
                          user.Email ??
                          user.Id;
        var email = userClaims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value ?? user.Email;

        var identity = new ClaimsIdentity(
            authenticationType: CookieAuthenticationDefaults.AuthenticationScheme,
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
        identity.AddClaim(new Claim(Claims.Subject, user.Id));
        identity.AddClaim(new Claim(ClaimTypes.Name, displayName));

        if (!string.IsNullOrWhiteSpace(email))
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, email));
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = returnUrl
        };

        return Results.SignIn(
            new ClaimsPrincipal(identity),
            properties,
            CookieAuthenticationDefaults.AuthenticationScheme);
    });

    app.MapGet("dev/users/new", (HttpContext context) =>
    {
        var error = context.Request.Query["error"].ToString();
        var message = context.Request.Query["message"].ToString();

        return Results.Content(BuildDevCreateUserHtml(error, message), "text/html");
    });

    app.MapPost("dev/users/new", async (HttpContext context, UserManager<User> userManager) =>
    {
        if (!context.Request.HasFormContentType)
        {
            return Results.BadRequest("Expected form body.");
        }

        var form = await context.Request.ReadFormAsync();
        var login = form["login"].ToString().Trim();
        var email = form["email"].ToString().Trim();
        var password = form["password"].ToString();

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            return Results.Redirect($"/dev/users/new?error={Uri.EscapeDataString("Login and password are required.")}");
        }

        var user = new User
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = login,
            Email = string.IsNullOrWhiteSpace(email) ? null : email
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var details = string.Join(" | ", result.Errors.Select(error => error.Description));
            return Results.Redirect($"/dev/users/new?error={Uri.EscapeDataString(details)}");
        }

        return Results.Redirect($"/dev/users/new?message={Uri.EscapeDataString($"User '{login}' created.")}");
    });

    app.MapGet("dev/logout", () => Results.Content(BuildDevLogoutHtml(), "text/html"));
    app.MapPost("dev/logout", async (HttpContext context) =>
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect($"/dev/login?message={Uri.EscapeDataString("Logged out.")}");
    });
}

app.MapMethods("callback/login/google", [HttpMethods.Get, HttpMethods.Post], async (HttpContext context, UserManager<User> userManager) =>
{
    var result = await context.AuthenticateAsync(Providers.Google);
    if (result.Succeeded != true || result.Principal is null)
    {
        return Results.BadRequest("External authentication error.");
    }

    var providerUserId = result.Principal.FindFirst("sub")?.Value ??
                         result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrWhiteSpace(providerUserId))
    {
        return Results.BadRequest("Google subject claim is missing.");
    }

    var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value ??
                result.Principal.FindFirst(Claims.Email)?.Value;
    var givenName = result.Principal.FindFirst(ClaimTypes.GivenName)?.Value ??
                    result.Principal.FindFirst(Claims.GivenName)?.Value;
    var surname = result.Principal.FindFirst(ClaimTypes.Surname)?.Value ??
                  result.Principal.FindFirst(Claims.FamilyName)?.Value;
    var displayName = result.Principal.FindFirst(ClaimTypes.Name)?.Value ??
                      result.Principal.FindFirst(Claims.Name)?.Value ??
                      email ??
                      providerUserId;

    var user = await userManager.FindByIdAsync(providerUserId);
    if (user is null)
    {
        user = new User
        {
            Id = providerUserId,
            UserName = email ?? providerUserId,
            Email = email
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            return Results.BadRequest(string.Join(" | ", createResult.Errors.Select(error => error.Description)));
        }
    }

    var claimsToAdd = new List<Claim>();
    var existingClaims = await userManager.GetClaimsAsync(user);

    AddClaimIfMissing(existingClaims, claimsToAdd, ClaimTypes.Email, email);
    AddClaimIfMissing(existingClaims, claimsToAdd, ClaimTypes.Name, displayName);
    AddClaimIfMissing(existingClaims, claimsToAdd, ClaimTypes.GivenName, givenName);
    AddClaimIfMissing(existingClaims, claimsToAdd, ClaimTypes.Surname, surname);

    if (claimsToAdd.Count > 0)
    {
        await userManager.AddClaimsAsync(user, claimsToAdd);
    }

    var identity = new ClaimsIdentity(
        authenticationType: CookieAuthenticationDefaults.AuthenticationScheme,
        nameType: ClaimTypes.Name,
        roleType: ClaimTypes.Role);

    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
    identity.AddClaim(new Claim(Claims.Subject, user.Id));
    identity.AddClaim(new Claim(ClaimTypes.Name, displayName));

    if (!string.IsNullOrWhiteSpace(email))
    {
        identity.AddClaim(new Claim(ClaimTypes.Email, email));
    }

    var redirectUri = result.Properties?.RedirectUri;
    if (string.IsNullOrWhiteSpace(redirectUri) &&
        result.Properties?.Items.TryGetValue("return_url", out var returnUrl) == true)
    {
        redirectUri = returnUrl;
    }

    var properties = new AuthenticationProperties();
    if (!string.IsNullOrWhiteSpace(redirectUri))
    {
        properties.RedirectUri = redirectUri;
    }

    return Results.SignIn(
        new ClaimsPrincipal(identity),
        properties,
        CookieAuthenticationDefaults.AuthenticationScheme);
});

app.MapMethods("connect/authorize", [HttpMethods.Get, HttpMethods.Post], async (
    HttpContext context,
    UserManager<User> userManager,
    IOpenIddictScopeManager scopeManager) =>
{
    var principal = (await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme))?.Principal;
    if (principal is not { Identity.IsAuthenticated: true })
    {
        if (app.Environment.IsDevelopment())
        {
            var devReturnUrl = context.Request.GetEncodedUrl();
            return Results.Redirect($"/dev/login?returnUrl={Uri.EscapeDataString(devReturnUrl)}");
        }

        if (!googleEnabled)
        {
            return Results.BadRequest("Google provider is not configured. Set Authentication:Google:ClientId and ClientSecret.");
        }

        var googleReturnUrl = context.Request.GetEncodedUrl();
        var properties = new AuthenticationProperties
        {
            RedirectUri = googleReturnUrl
        };
        properties.Items["return_url"] = googleReturnUrl;

        return Results.Challenge(properties, [Providers.Google]);
    }

    var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                 principal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;

    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.BadRequest("Authenticated user does not have a subject identifier.");
    }

    var user = await userManager.FindByIdAsync(userId);
    if (user is null)
    {
        return Results.BadRequest("Authenticated user was not found in identity store.");
    }

    var userClaims = await userManager.GetClaimsAsync(user);

    var identity = new ClaimsIdentity(
        authenticationType: TokenValidationParameters.DefaultAuthenticationType,
        nameType: OpenIddictConstants.Claims.Name,
        roleType: OpenIddictConstants.Claims.Role);

    identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id).SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

    var username = user.UserName ?? user.Email ?? user.Id;
    identity.AddClaim(new Claim(OpenIddictConstants.Claims.PreferredUsername, username).SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

    var name = userClaims.FirstOrDefault(claim => claim.Type == ClaimTypes.Name)?.Value ?? username;
    identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, name).SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

    var email = userClaims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value ?? user.Email;
    if (!string.IsNullOrWhiteSpace(email))
    {
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, email).SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
    }

    var givenName = userClaims.FirstOrDefault(claim => claim.Type == ClaimTypes.GivenName)?.Value;
    if (!string.IsNullOrWhiteSpace(givenName))
    {
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.GivenName, givenName).SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
    }

    var familyName = userClaims.FirstOrDefault(claim => claim.Type == ClaimTypes.Surname)?.Value;
    if (!string.IsNullOrWhiteSpace(familyName))
    {
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.FamilyName, familyName).SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
    }

    identity.SetScopes(await ResolveScopesAsync(context));
    identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

    return Results.SignIn(new ClaimsPrincipal(identity), properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

app.MapMethods("connect/logout", [HttpMethods.Get, HttpMethods.Post], async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    return Results.SignOut(
        properties: new AuthenticationProperties
        {
            RedirectUri = "/"
        },
        authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
});

app.MapPost("connect/token", async (HttpContext context, IOpenIddictScopeManager scopeManager) =>
{
    var form = context.Request.HasFormContentType
        ? await context.Request.ReadFormAsync()
        : null;

    var grantType = form?["grant_type"].ToString();

    if (string.Equals(grantType, GrantTypes.AuthorizationCode, StringComparison.Ordinal) ||
        string.Equals(grantType, GrantTypes.RefreshToken, StringComparison.Ordinal))
    {
        var authenticationResult = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (authenticationResult.Succeeded != true || authenticationResult.Principal is null)
        {
            return Results.Forbid(authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }

        return Results.SignIn(authenticationResult.Principal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    if (string.Equals(grantType, GrantTypes.ClientCredentials, StringComparison.Ordinal))
    {
        var clientId = form?["client_id"].ToString();
        if (string.IsNullOrWhiteSpace(clientId))
        {
            clientId = TryReadClientIdFromBasicAuthorizationHeader(context);
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Results.BadRequest("Client identifier cannot be resolved.");
        }

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, clientId).SetDestinations(Destinations.AccessToken));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, clientId).SetDestinations(Destinations.AccessToken));

        identity.SetScopes(ParseScopes(form?["scope"].ToString()));
        identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

        return Results.SignIn(new ClaimsPrincipal(identity), properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    return Results.BadRequest("Unsupported grant type.");
});

await using (var scope = app.Services.CreateAsyncScope())
{
    var identityContext = scope.ServiceProvider.GetRequiredService<IdentityStoreContext>();
    await identityContext.Database.MigrateAsync();

    var authStoreContext = scope.ServiceProvider.GetRequiredService<AuthStoreContext>();
    await authStoreContext.Database.EnsureCreatedAsync();

    await EnsureScopesAndApplications(scope.ServiceProvider);
}

await app.RunAsync();

static void AddClaimIfMissing(IEnumerable<Claim> existingClaims, IList<Claim> newClaims, string type, string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return;
    }

    var exists = existingClaims.Any(claim => claim.Type == type && claim.Value == value);
    if (!exists)
    {
        newClaims.Add(new Claim(type, value));
    }
}

static string GetValidatedReturnUrl(HttpContext context, string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl))
    {
        return "/";
    }

    if (Uri.TryCreate(returnUrl, UriKind.Relative, out var relativeUri) && !relativeUri.IsAbsoluteUri)
    {
        if (returnUrl.StartsWith('/'))
        {
            return returnUrl;
        }

        return "/";
    }

    if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var absoluteUri))
    {
        return "/";
    }

    var requestHost = context.Request.Host;
    var sameHost = string.Equals(absoluteUri.Host, requestHost.Host, StringComparison.OrdinalIgnoreCase);
    var samePort = absoluteUri.Port == (requestHost.Port ?? (context.Request.IsHttps ? 443 : 80));
    var sameScheme = string.Equals(absoluteUri.Scheme, context.Request.Scheme, StringComparison.OrdinalIgnoreCase);

    return sameHost && samePort && sameScheme
        ? absoluteUri.ToString()
        : "/";
}

static string BuildDevLoginHtml(string returnUrl, string? error, string? message)
{
    var errorHtml = string.IsNullOrWhiteSpace(error)
        ? string.Empty
        : $"<p>{WebUtility.HtmlEncode(error)}</p>";

    var messageHtml = string.IsNullOrWhiteSpace(message)
        ? string.Empty
        : $"<p>{WebUtility.HtmlEncode(message)}</p>";

    var encodedReturnUrl = WebUtility.HtmlEncode(returnUrl);

    return $$"""
<!doctype html>
<html>
<body>
<h1>Dev Login</h1>
{{errorHtml}}
{{messageHtml}}
<form method="post" action="/dev/login">
  <input type="hidden" name="returnUrl" value="{{encodedReturnUrl}}" />
  <label>Login</label>
  <input name="login" autocomplete="username" />
  <br />
  <label>Password</label>
  <input type="password" name="password" autocomplete="current-password" />
  <br />
  <button type="submit">Log in</button>
</form>
<p><a href="/dev/users/new">Create test user</a></p>
<p><a href="/dev/logout">Logout</a></p>
</body>
</html>
""";
}

static string BuildDevCreateUserHtml(string? error, string? message)
{
    var errorHtml = string.IsNullOrWhiteSpace(error)
        ? string.Empty
        : $"<p>{WebUtility.HtmlEncode(error)}</p>";

    var messageHtml = string.IsNullOrWhiteSpace(message)
        ? string.Empty
        : $"<p>{WebUtility.HtmlEncode(message)}</p>";

    return $$"""
<!doctype html>
<html>
<body>
<h1>Create Dev User</h1>
{{errorHtml}}
{{messageHtml}}
<form method="post" action="/dev/users/new">
  <label>Login</label>
  <input name="login" autocomplete="username" />
  <br />
  <label>Email (optional)</label>
  <input name="email" autocomplete="email" />
  <br />
  <label>Password</label>
  <input type="password" name="password" autocomplete="new-password" />
  <br />
  <button type="submit">Create user</button>
</form>
<p><a href="/dev/login">Back to login</a></p>
</body>
</html>
""";
}

static string BuildDevLogoutHtml()
{
    return """
<!doctype html>
<html>
<body>
<h1>Dev Logout</h1>
<form method="post" action="/dev/logout">
  <button type="submit">Logout</button>
</form>
</body>
</html>
""";
}

static async Task EnsureScopesAndApplications(IServiceProvider services)
{
    var scopeManager = services.GetRequiredService<IOpenIddictScopeManager>();
    var applicationManager = services.GetRequiredService<IOpenIddictApplicationManager>();

    await UpsertScope(
        scopeManager,
        new OpenIddictScopeDescriptor
        {
            Name = "tbdancedanceapi.read",
            DisplayName = "TB DanceDance API - read",
            Resources =
            {
                "tbdancedanceapi"
            }
        });

    await UpsertScope(
        scopeManager,
        new OpenIddictScopeDescriptor
        {
            Name = "tbdancedanceapi.convert",
            DisplayName = "TB DanceDance API - converter",
            Resources =
            {
                "tbdancedanceapi"
            }
        });

    var frontClientDescriptor = new OpenIddictApplicationDescriptor
    {
        ClientId = "tbdancedancefront",
        ClientType = ClientTypes.Public,
        RedirectUris =
        {
            new Uri("http://localhost:3000/callback"),
            new Uri("http://localhost:4200/callback"),
            new Uri("http://localhost:5112/signin-callback.html"),
            new Uri("http://localhost:5112/signin-silent-callback.html"),
            new Uri("http://localhost:5112/index.html")
        },
        PostLogoutRedirectUris =
        {
            new Uri("http://localhost:3000")
        },
        Permissions =
        {
            Permissions.Endpoints.Authorization,
            Permissions.Endpoints.EndSession,
            Permissions.Endpoints.Token,
            Permissions.GrantTypes.AuthorizationCode,
            Permissions.GrantTypes.RefreshToken,
            Permissions.ResponseTypes.Code,
        },
        Requirements =
        {
            Requirements.Features.ProofKeyForCodeExchange
        }
    };

    frontClientDescriptor.AddScopePermissions(Scopes.OpenId, Scopes.Profile, Scopes.Email, Scopes.OfflineAccess, "tbdancedanceapi.read");

    await UpsertApplication(applicationManager, frontClientDescriptor);

    var converterClientDescriptor = new OpenIddictApplicationDescriptor
    {
        ClientId = "tbdancedanceconverter",
        ClientType = ClientTypes.Confidential,
        ClientSecret = "other",
        DisplayName = "TB DanceDance Converter Daemon",
        Permissions =
        {
            Permissions.Endpoints.Token,
            Permissions.GrantTypes.ClientCredentials
        }
    };

    converterClientDescriptor.AddScopePermissions("tbdancedanceapi.convert");

    await UpsertApplication(applicationManager, converterClientDescriptor);
}

static async Task UpsertScope(IOpenIddictScopeManager scopeManager, OpenIddictScopeDescriptor descriptor)
{
    if (string.IsNullOrWhiteSpace(descriptor.Name))
    {
        throw new InvalidOperationException("OpenIddict scope descriptor name cannot be empty.");
    }

    var existingScope = await scopeManager.FindByNameAsync(descriptor.Name);
    if (existingScope is null)
    {
        await scopeManager.CreateAsync(descriptor);
        return;
    }

    await scopeManager.PopulateAsync(descriptor, existingScope);
    await scopeManager.UpdateAsync(existingScope, descriptor);
}

static async Task UpsertApplication(IOpenIddictApplicationManager applicationManager, OpenIddictApplicationDescriptor descriptor)
{
    if (string.IsNullOrWhiteSpace(descriptor.ClientId))
    {
        throw new InvalidOperationException("OpenIddict application client id cannot be empty.");
    }

    var existingApplication = await applicationManager.FindByClientIdAsync(descriptor.ClientId);
    if (existingApplication is null)
    {
        await applicationManager.CreateAsync(descriptor);
        return;
    }

    await applicationManager.PopulateAsync(descriptor, existingApplication);
    await applicationManager.UpdateAsync(existingApplication, descriptor);
}

static async Task<IEnumerable<string>> ResolveScopesAsync(HttpContext context)
{
    var scope = context.Request.Query["scope"].ToString();
    if (!string.IsNullOrWhiteSpace(scope))
    {
        return ParseScopes(scope);
    }

    if (context.Request.HasFormContentType)
    {
        var form = await context.Request.ReadFormAsync();
        return ParseScopes(form["scope"].ToString());
    }

    return [];
}

static IEnumerable<string> ParseScopes(string? scope)
{
    return string.IsNullOrWhiteSpace(scope)
        ? []
        : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

static string? TryReadClientIdFromBasicAuthorizationHeader(HttpContext context)
{
    var authorizationHeader = context.Request.Headers.Authorization.ToString();
    if (!authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    var payload = authorizationHeader["Basic ".Length..].Trim();

    string decoded;
    try
    {
        var bytes = Convert.FromBase64String(payload);
        decoded = System.Text.Encoding.UTF8.GetString(bytes);
    }
    catch
    {
        return null;
    }

    var separatorIndex = decoded.IndexOf(':');
    if (separatorIndex <= 0)
    {
        return null;
    }

    return decoded[..separatorIndex];
}
