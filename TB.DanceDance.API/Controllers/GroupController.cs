using Domain.Services;
using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Mappers;

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

        var dict = new Dictionary<Guid, (string, List<VideoInformationModel>)>();

        foreach (var video in videos)
        {
            var videoDetails = ContractMappers.MapToVideoInformation(video);

            if (!dict.ContainsKey(video.GroupId))
            {
                dict[video.GroupId] = new(video.GroupName, new List<VideoInformationModel>() { videoDetails });
            }
            else
            {
                dict[video.GroupId].Item2.Add(videoDetails);
            }
        }

        var map = dict.Select((k) => new GroupWithVideosResponse()
        {
            GroupId = k.Key,
            GroupName = k.Value.Item1,
            Videos = k.Value.Item2
        });

        return Ok(map);
    }
}
