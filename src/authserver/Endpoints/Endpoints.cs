using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using OpenIddict.Server.AspNetCore;
using TB.Auth.Web.Endpoints.Handlers;
using TB.Auth.Web.Endpoints.Handlers.DevelopmentEndpoints;

namespace TB.Auth.Web.Endpoints;

public static class Endpoints
{
    extension(WebApplication app)
    {
        public void MapDevelopmentEndpoints()
        {
            app.MapGet("dev/login", GetDevLoginHandler.Handle);
            app.MapPost("dev/login", PostDevLoginHandler.HandleAsync);
            app.MapGet("dev/users/new", GetNewUserHandler.Handle);
            app.MapPost("dev/users/new", PostNewUserHandler.HandleAsync);
            app.MapGet("dev/logout", () => Results.Content(new HtmlBuilder().BuildDevLogoutHtml(), "text/html"));
            app.MapPost("dev/logout", async (HttpContext context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect($"/dev/login?message={Uri.EscapeDataString("Logged out.")}");
            });
        }

        public void MapEndpoints(bool googleEnabled, bool allowPasswordLogin = false)
        {
            app.ConfigureConnectAuthorize(googleEnabled, allowPasswordLogin);
            app.MapPost("connect/token", TokenHandler.HandleAsync);
            app.MapGet("policy/dancedanceapp", () => Results.Text("Privacy policy page is not implemented yet."));
            app.MapMethods("callback/login/google", 
                [HttpMethods.Get, HttpMethods.Post],
                GoogleLoginHandler.HandleAsync);
            app.MapMethods("connect/logout", [HttpMethods.Get, HttpMethods.Post], async (HttpContext context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                return Results.SignOut(
                    properties: new AuthenticationProperties { RedirectUri = "/" },
                    authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
            });
        }
        
        private void ConfigureConnectAuthorize(bool googleEnabled, bool allowPasswordLogin)
        {
            const string endpoint = "connect/authorize";
            if (allowPasswordLogin)
            {
                app.MapMethods(endpoint, [HttpMethods.Get, HttpMethods.Post], ConnectAuthorizeHandler.HandleAsync);
                return;
            }
            
            if (!googleEnabled)
            {
                app.MapGet(endpoint,
                    () => Results.BadRequest(
                        "Google provider is not configured. Set Authentication:Google:ClientId and ClientSecret."));
            }
            else
            {
                app.MapMethods(endpoint, [HttpMethods.Get, HttpMethods.Post],
                    ConnectAuthorizeHandler.HandleAsync);
            }
        }
    }
}
