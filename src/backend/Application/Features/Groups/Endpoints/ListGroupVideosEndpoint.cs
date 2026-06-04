using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Groups;

namespace Application.Features.Groups.Endpoints;

public class ListGroupVideosEndpoint : Endpoint<ListGroupVideosRequest, ListGroupVideosResponse>
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
    
    public override async Task HandleAsync(ListGroupVideosRequest request, CancellationToken ct)
    {
        var userId = User.GetSubject();
        var videos = await groupService.GetAllVideos(userId, request.GroupId, ct);

        var response = new ListGroupVideosResponse() { Videos = videos };
        
        await Send.OkAsync(response, ct);
    }
}