using Application.Extensions;
using Domain.Entities;
using FastEndpoints;
using FluentValidation;
using TB.DanceDance.API.Contracts.Features.Videos;

namespace Application.Features.Videos.Endpoints.Videos;

public class UpdateCommentSettingsValidator : Validator<UpdateCommentSettingsRequest>
{
    public UpdateCommentSettingsValidator()
    {
        RuleFor(x => x.CommentVisibility)
            .InclusiveBetween(0, 2)
            .WithMessage("CommentVisibility must be 0 (Public), 1 (AuthenticatedOnly) or 2 (OwnerOnly).");
    }
}

public class UpdateCommentSettingsEndpoint : Endpoint<UpdateCommentSettingsRequest>
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
        var videoId = Route<Guid>("videoId");

        // CommentVisibility range is enforced by UpdateCommentSettingsValidator; the service performs
        // the ownership authorization check and returns false (→ 404) when the user may not update it.
        var result = await videoService.UpdateCommentVisibilityAsync(
            videoId,
            userId,
            (CommentVisibility)req.CommentVisibility,
            ct);

        if (!result)
        {
            await Send.NotFoundAsync(ct);
        }
        else
        {
            await Send.NoContentAsync(ct);
        }
    }
}