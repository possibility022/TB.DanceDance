using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TB.Auth.Web.Identity;

namespace TB.Auth.Web.Endpoints.Handlers.DevelopmentEndpoints;

public static class PostNewUserHandler
{
    public static async Task<IResult> HandleAsync(HttpContext context, UserManager<User> userManager)
    {
        if (!context.Request.HasFormContentType)
        {
            return Results.BadRequest("Expected form body.");
        }

        var form = await context.Request.ReadFormAsync();
        var login = form["login"].ToString().Trim();
        var email = form["email"].ToString().Trim();
        var firstName = form["firstName"].ToString().Trim();
        var lastName = form["lastName"].ToString().Trim();
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

        var claims = new List<Claim>();
        if (!string.IsNullOrWhiteSpace(firstName))
            claims.Add(new Claim(ClaimTypes.GivenName, firstName));
        if (!string.IsNullOrWhiteSpace(lastName))
            claims.Add(new Claim(ClaimTypes.Surname, lastName));
        if (!string.IsNullOrWhiteSpace(email))
            claims.Add(new Claim(ClaimTypes.Email, email));

        if (claims.Count > 0)
            await userManager.AddClaimsAsync(user, claims);

        return Results.Redirect($"/dev/users/new?message={Uri.EscapeDataString($"User '{login}' created.")}");
    }
}