using Application.Extensions;
using Application.Features.Groups.Models;
using FastEndpoints;

namespace Application.Features.Groups.Endpoints;

public record ListGroupVideosRequest(Guid GroupId);

public class ListGroupVideos : Endpoint<ListGroupVideosRequest, ListGroupVideosResponse>
{
    private readonly IGroupService groupService;

    public ListGroupVideos(IGroupService groupService)
    {
        this.groupService = groupService;
    }
    
    public override void Configure()
    {
        Get(ApiRoutes.Groups.VideosForGroup);
    }
    
    public override async Task HandleAsync(ListGroupVideosRequest request, CancellationToken ct)
    {
        var userId = User.GetSubject();
        var videos = await groupService.GetAllVideos(userId, request.GroupId, ct);

        var response = new ListGroupVideosResponse() { Videos = videos };
        
        await Send.OkAsync(response, ct);
    }
}