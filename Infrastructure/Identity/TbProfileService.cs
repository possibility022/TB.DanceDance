using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Infrastructure.Identity;
public class TbProfileService : DefaultProfileService, IProfileService
{
    private readonly UserManager<User> userManager;

    public TbProfileService(UserManager<User> userManager, ILogger<TbProfileService> logger) : base(logger)
    {
        this.userManager = userManager;
    }

    async Task IProfileService.GetProfileDataAsync(ProfileDataRequestContext context)
    {
        await base.GetProfileDataAsync(context);

        var sub = context.Subject.Claims.FirstOrDefault(r => r.Type == "sub");

        if (sub == null)
            throw new TbIdentityException("'sub' claim type was not found.");

        var user = await userManager.FindByIdAsync(sub.Value);

        if (user == null)
            throw new TbIdentityException("User not found.");

        var claims = await userManager.GetClaimsAsync(user);
        var name = claims.FirstOrDefault(r => r.Type == ClaimTypes.Name);

        if (name != null)
            context.IssuedClaims.Add(new Claim("name", name.Value));
    }

    Task IProfileService.IsActiveAsync(IsActiveContext context)
    {
        return Task.CompletedTask;
    }
}
