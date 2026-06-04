using Application.Extensions;
using Domain.Entities;
using FastEndpoints;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Videos.Endpoints.Videos;

public class UpdateCommentSettingsRequest
{
    public Guid VideoId { get; init; }
    /// <summary>
    /// Controls who can see comments on this video.
    /// 0 = Public (anyone with link), 1 = AuthenticatedOnly, 2 = OwnerOnly
    /// </summary>
    [Required]
    [Range(0, 2)]
    public int CommentVisibility { get; set; }
}

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