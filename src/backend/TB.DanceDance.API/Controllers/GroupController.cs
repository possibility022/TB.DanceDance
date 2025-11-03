using Domain.Entities;
using Domain.Services;
using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> GetVideosAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        var videos = await groupService
            .GetUserVideosForAllGroups(userId, cancellationToken);

        var response = MapToVideoForGroupInfoResponse(videos);

        return Ok(response);
    }

    [HttpGet]
    [Route(ApiEndpoints.Group.VideosForGroup)]
    public async Task<IActionResult> GetVideosPerGroup(Guid groupId, CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        var videos = await groupService
            .GetUserVideosForGroup(userId, groupId, cancellationToken);

        var videosByGroups = MapToVideoForGroupInfoResponse(videos);
        var group = videosByGroups.FirstOrDefault();

        if (group == null)
            return NotFound();

        return Ok(group);
    }

    private static IEnumerable<GroupWithVideosResponse> MapToVideoForGroupInfoResponse(IEnumerable<VideoFromGroupInfo> videos)
    {
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

        return map;
    }
}
