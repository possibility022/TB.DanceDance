using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Groups;
using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Models;

namespace Application.Features.Groups.Endpoints;

public class ListGroupVideosEndpoint : Endpoint<ListGroupVideosRequest, PagedResponse<VideoFromGroupInformation>>
{
    private readonly IGroupService groupService;

    public ListGroupVideosEndpoint(IGroupService groupService)
    {
        this.groupService = groupService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Groups.VideosForGroup);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(ListGroupVideosRequest req, CancellationToken ct)
    {
        var groupId = Route<Guid>("groupId");
        var userId = User.GetSubject();
        var pageNumber = req.NormalizedPage;
        var pageSize = req.NormalizedPageSize;

        var (videos, totalCount) = await groupService.GetAllVideos(userId, groupId, pageNumber, pageSize, ct);

        var response = new PagedResponse<VideoFromGroupInformation>
        {
            Items = videos.ToArray(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        await Send.OkAsync(response, ct);
    }
}
