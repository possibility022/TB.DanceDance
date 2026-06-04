using Application.Extensions;
using Application.Features.Groups.Models;
using FastEndpoints;

namespace Application.Features.Groups.Endpoints;

public class ListAllGroupVideosEndpoint : EndpointWithoutRequest<ListGroupVideosResponse>
{
    private readonly IGroupService groupService;

    public ListAllGroupVideosEndpoint(IGroupService groupService)
    {
        this.groupService = groupService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Groups.Videos);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();

        var videos = await groupService
            .GetAllVideos(userId, ct);

        var response = new ListGroupVideosResponse() { Videos = videos };

        await Send.OkAsync(response, ct);
    }
}