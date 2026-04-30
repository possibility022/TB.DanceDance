using Infrastructure.Identity;
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
    }
}