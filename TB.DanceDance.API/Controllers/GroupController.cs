using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Mappers;
using TB.DanceDance.Identity.IdentityResources;
using TB.DanceDance.Services;

namespace TB.DanceDance.API.Controllers;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
public class GroupController : Controller
{
    private readonly IGroupService groupService;

    public GroupController(IGroupService groupService)
    {
        this.groupService = groupService;
    }

    [HttpGet]
    [Route(ApiEndpoints.Group.Videos)]
    public async Task<IActionResult> GetVideosAsync()
    {
        var userId = User.GetSubject();

        var videos = await groupService
            .GetUserVideosFromGroups(userId)
            .ToListAsync();

        var results = videos
            .Select(r => ContractMappers.MapToVideoInformation(r))
            .ToList();

        return Ok(results);
    }
}
