using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.API.Contracts.Features.Groups;
using TB.DanceDance.API.Contracts.Features.Videos;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Mappers;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;

namespace TB.DanceDance.API.Features.Groups;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
public class GroupController : Controller
{
    private readonly IRequestHandler<GetUserGroupMembershipsQuery, IReadOnlyCollection<GroupMembershipDto>> getUserGroupMembershipsQuery;
    private readonly IRequestHandler<GetAllGroupsQuery, IReadOnlyCollection<GroupDto>> getAllGroupsQuery;
    private readonly IRequestHandler<GetGroupByIdQuery, GroupDto?> getGroupByIdQuery;
    private readonly IRequestHandler<ViewVideosFromGroupQuery, IReadOnlyCollection<VideoDto>> viewVideosFromGroupQuery;

    public GroupController(
        IRequestHandler<GetUserGroupMembershipsQuery, IReadOnlyCollection<GroupMembershipDto>> getUserGroupMembershipsQuery,
        IRequestHandler<GetAllGroupsQuery, IReadOnlyCollection<GroupDto>> getAllGroupsQuery,
        IRequestHandler<GetGroupByIdQuery, GroupDto?> getGroupByIdQuery,
        IRequestHandler<ViewVideosFromGroupQuery, IReadOnlyCollection<VideoDto>> viewVideosFromGroupQuery)
    {
        this.getUserGroupMembershipsQuery = getUserGroupMembershipsQuery;
        this.getAllGroupsQuery = getAllGroupsQuery;
        this.getGroupByIdQuery = getGroupByIdQuery;
        this.viewVideosFromGroupQuery = viewVideosFromGroupQuery;
    }

    [HttpGet]
    [Route(GroupRoutes.Videos)]
    public async Task<IActionResult> GetVideosAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        var memberships = await getUserGroupMembershipsQuery.HandleAsync(new GetUserGroupMembershipsQuery(userId), cancellationToken);
        var allGroups = await getAllGroupsQuery.HandleAsync(new GetAllGroupsQuery(), cancellationToken);
        var groupsById = allGroups.ToDictionary(g => g.Id);

        var response = new List<GroupWithVideosResponse>();

        foreach (var membership in memberships)
        {
            if (!groupsById.TryGetValue(membership.GroupId, out var group))
                continue;

            var videos = await viewVideosFromGroupQuery.HandleAsync(new ViewVideosFromGroupQuery(userId, membership.GroupId), cancellationToken);

            if (videos.Count == 0)
                continue;

            response.Add(MapToGroupWithVideosResponse(group, videos));
        }

        return Ok(response);
    }

    [HttpGet]
    [Route(GroupRoutes.VideosForGroup)]
    public async Task<IActionResult> GetVideosPerGroup(Guid groupId, CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        var videos = await viewVideosFromGroupQuery.HandleAsync(new ViewVideosFromGroupQuery(userId, groupId), cancellationToken);

        if (videos.Count == 0)
            return NotFound();

        var group = await getGroupByIdQuery.HandleAsync(new GetGroupByIdQuery { Id = groupId }, cancellationToken);

        if (group == null)
            return NotFound();

        return Ok(MapToGroupWithVideosResponse(group, videos));
    }

    private static GroupWithVideosResponse MapToGroupWithVideosResponse(GroupDto group, IReadOnlyCollection<VideoDto> videos)
    {
        return new GroupWithVideosResponse()
        {
            GroupId = group.Id,
            GroupName = group.Name,
            SeasonStart = group.SeasonStart.ToDateTime(TimeOnly.MinValue),
            SeasonEnd = group.SeasonEnd.ToDateTime(TimeOnly.MaxValue),
            Videos = videos.Select(v => (VideoInformationModel)ContractMappers.MapToVideoInformation(v)).ToList(),
        };
    }
}
