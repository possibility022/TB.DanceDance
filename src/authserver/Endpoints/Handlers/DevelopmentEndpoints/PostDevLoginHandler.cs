using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace TB.Auth.Web.Endpoints.Handlers.DevelopmentEndpoints;

public static class PostDevLoginHandler
{
    public static async Task<IResult> HandleAsync(HttpContext context, UserManager<User> userManager)
    {
        if (!context.Request.HasFormContentType)
        {
            return Results.BadRequest("Expected form body.");
        }

        var form = await context.Request.ReadFormAsync();
        var login = form["login"].ToString().Trim();
        var password = form["password"].ToString();
        
        var routeBuilder = new RouteBuilder(context);
        
        var returnUrl = routeBuilder.GetValidatedReturnUrl(form["returnUrl"].ToString());

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
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id));
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
    }
}