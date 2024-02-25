using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TB.DanceDance.Core.Exceptions;
using TB.DanceDance.Identity;

namespace IdentityServerHost.Quickstart.UI;

[SecurityHeaders]
[AllowAnonymous]
[Route("[controller]/[action]")]
public class ExternalController : Controller
{
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IClientStore _clientStore;
    private readonly ILogger<ExternalController> _logger;
    private readonly IdentityServer4.Services.IEventService _events;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public ExternalController(
        IIdentityServerInteractionService interaction,
        IClientStore clientStore,
        IdentityServer4.Services.IEventService events,
        ILogger<ExternalController> logger,
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {

        _interaction = interaction;
        _clientStore = clientStore;
        _logger = logger;
        _events = events;
        this._userManager = userManager;
        this._signInManager = signInManager;
    }

    /// <summary>
    /// initiate roundtrip to external authentication provider
    /// </summary>
    [HttpGet]
    public IActionResult Challenge(string scheme, string returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl)) returnUrl = "~/";

        // validate returnUrl - either it is a valid OIDC URL or back to a local page
        if (Url.IsLocalUrl(returnUrl) == false && _interaction.IsValidReturnUrl(returnUrl) == false)
        {
            // user might have clicked on a malicious link - should be logged
            throw new Exception("invalid return URL");
        }

        // start challenge and roundtrip the return URL and scheme 
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(Callback)),
            Items =
            {
                { "returnUrl", returnUrl },
                { "scheme", scheme },
            }
        };

        return Challenge(props, scheme);

    }

    /// <summary>
    /// Post processing of external authentication
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Callback()
    {
        // read external identity from the temporary cookie
        var result = await HttpContext.AuthenticateAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
        if (result?.Succeeded != true)
        {
            throw new Exception("External authentication error");
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var externalClaims = result.Principal.Claims.Select(c => $"{c.Type}: {c.Value}");
            _logger.LogDebug("External claims: {@claims}", externalClaims);
        }

        // lookup our user and external provider info
        var (user, provider, providerUserId, claims) = await FindUserFromExternalProviderAsync(result);
        if (user == null)
        {
            user = await AutoProvisionUserAsync(provider, providerUserId, claims);
        }

        // this allows us to collect any additional claims or properties
        // for the specific protocols used and store them in the local auth cookie.
        // this is typically used to store data needed for signout from those protocols.
        var additionalLocalClaims = new List<Claim>();
        var localSignInProps = new AuthenticationProperties();
        ProcessLoginCallback(result, additionalLocalClaims, localSignInProps);

        // issue authentication cookie for user
        var isuser = new IdentityServerUser(user.Id)
        {
            DisplayName = user.UserName,
            IdentityProvider = provider,
            AdditionalClaims = additionalLocalClaims
        };

        await HttpContext.SignInAsync(isuser, localSignInProps);

        // delete temporary cookie used during external authentication
        await HttpContext.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);

        // retrieve return URL
        var returnUrl = result.Properties.Items["returnUrl"] ?? "~/";

        // check if external login is in the context of an OIDC request
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        await _events.RaiseAsync(new UserLoginSuccessEvent(provider, providerUserId, user.Id, user.UserName, true, context?.Client.ClientId));

        if (context != null)
        {
            if (context.IsNativeClient())
            {
                // The client is native, so this change in how to
                // return the response is for better UX for the end user.
                return this.LoadingPage("Redirect", returnUrl);
            }
        }

        return Redirect(returnUrl);
    }

    private async Task<(User? user, string provider, string providerUserId, IEnumerable<Claim> claims)> FindUserFromExternalProviderAsync(AuthenticateResult result)
    {
        var externalUser = result.Principal;

        // try to determine the unique id of the external user (issued by the provider)
        // the most common claim type for that are the sub claim and the NameIdentifier
        // depending on the external provider, some other claim type might be used
        var userIdClaim = externalUser.FindFirst(JwtClaimTypes.Subject) ??
                          externalUser.FindFirst(ClaimTypes.NameIdentifier) ??
                          throw new Exception("Unknown userid");

        // remove the user id claim so we don't include it as an extra claim if/when we provision the user
        var claims = externalUser.Claims.ToList();
        claims.Remove(userIdClaim);

        var provider = result.Properties.Items["scheme"];
        var providerUserId = userIdClaim.Value;

        // find external user
        var user = await _userManager.FindByIdAsync(providerUserId);

        return (user, provider, providerUserId, claims);

    }

    private async Task<User> AutoProvisionUserAsync(string provider, string providerUserId, IEnumerable<Claim> claims)
    {
        (string ? email, string ? givenName, string ? surname) = GetValuesFromClaims(claims);

#pragma warning disable CS8601 // Possible null reference assignment.
        var user = new TB.DanceDance.Identity.User()
        {
            Id = providerUserId,
            Email = email,
            UserName = email,
            FirstName = givenName,
            LastName = surname,
            EmailConfirmed = true
        };
#pragma warning restore CS8601 // Possible null reference assignment.

        var res = await _userManager.CreateAsync(user);

        if (!res.Succeeded)
        {
            throw new AppException("User could not be created. " + string.Join('|', res.Errors.Select(r => r.Description)) + string.Join('|', res.Errors.Select(r => r.Code))); // todo handle it
        }

        return user;
    }

    // if the external login is OIDC-based, there are certain things we need to preserve to make logout work
    // this will be different for WS-Fed, SAML2p or other protocols
    private void ProcessLoginCallback(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
    {
        // if the external system sent a session id claim, copy it over
        // so we can use it for single sign-out
        var sid = externalResult.Principal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
        if (sid != null)
        {
            localClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
        }

        // if the external provider issued an id_token, we'll keep it for signout
        var idToken = externalResult.Properties.GetTokenValue("id_token");
        if (idToken != null)
        {
            localSignInProps.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = idToken } });
        }
    }

    private (string? email, string? givenName, string? surname) GetValuesFromClaims(IEnumerable<Claim> claims)
    {
        string? givenName = null;
        string? surname = null;
        string? email = null;

        foreach (var claim in claims)
        {
            if (claim.Type == ClaimTypes.GivenName)
            {
                givenName = claim.Value;
            }
            else if (claim.Type == ClaimTypes.Surname)
            {
                surname = claim.Value;
            }
            else if (claim.Type == ClaimTypes.Email)
            {
                email = claim.Value;
            }

            if (givenName != null && surname != null && email != null)
                break;
        }

        return (email, givenName, surname);
    }
}