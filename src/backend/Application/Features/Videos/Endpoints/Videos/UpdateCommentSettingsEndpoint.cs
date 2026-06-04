using Application.Extensions;
using Domain.Entities;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Videos;

namespace Application.Features.Videos.Endpoints.Videos;

public class UpdateCommentSettingsEndpoint : Endpoint<UpdateCommentSettingsRequest, EmptyResponse>
{
    private readonly IVideoService videoService;

    public UpdateCommentSettingsEndpoint(IVideoService videoService)
    {
        this.videoService = videoService;
    }
    
    public override void Configure()
    {
        Post(ApiRoutes.Video.UpdateCommentSettings);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(UpdateCommentSettingsRequest req, CancellationToken ct)
    {
        var userId = User.GetSubject();

        // todo, move validation/authorization to service?
        var result = await videoService.UpdateCommentVisibilityAsync(
            req.VideoId,
            userId,
            (CommentVisibility)req.CommentVisibility,
            ct);

        if (!result)
        {
            await Send.NotFoundAsync(ct);
        }
        else
        {
            await Send.OkAsync(ct);
        }
    }
}