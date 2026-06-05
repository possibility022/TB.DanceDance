using Application.Extensions;
using Application.Features.AccessManagement;
using FastEndpoints;
using FluentValidation;
using Microsoft.Extensions.Logging;
using TB.DanceDance.API.Contracts.Features.Videos;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Videos;

public class ProduceUploadUrlValidator : Validator<ProduceUploadUrlRequest>
{
    public ProduceUploadUrlValidator()
    {
        RuleFor(x => x.NameOfVideo)
            .NotEmpty().WithMessage("NameOfVideo is required.")
            .MinimumLength(5)
            .MaximumLength(100);

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("FileName is required.");

        RuleFor(x => x.SharingWithType)
            .IsInEnum()
            .NotEqual(SharingWithType.NotSpecified)
            .WithMessage("A valid sharing type (Group, Event or Private) is required.");

        When(x => x.SharingWithType is SharingWithType.Group or SharingWithType.Event, () =>
        {
            RuleFor(x => x.SharedWith)
                .NotNull()
                .WithMessage("SharedWith is required for Group and Event sharing types.");
        });

        When(x => x.SharingWithType == SharingWithType.Private, () =>
        {
            RuleFor(x => x.SharedWith)
                .Null()
                .WithMessage("SharedWith must be null for Private sharing type.");
        });
    }
}

public class ProduceUploadUrlEndpoint : Endpoint<ProduceUploadUrlRequest, ProduceUploadUrlResponse>
{
    private readonly IAccessService accessService;
    private readonly IVideoService videoService;
    private readonly ILogger<ProduceUploadUrlEndpoint> logger;

    public ProduceUploadUrlEndpoint(IAccessService accessService, IVideoService videoService, ILogger<ProduceUploadUrlEndpoint> logger)
    {
        this.accessService = accessService;
        this.videoService = videoService;
        this.logger = logger;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Video.GetUploadUrl);
        Policies(ApiScopes.Read);
    }

    public override async Task<Void> HandleAsync(ProduceUploadUrlRequest req, CancellationToken ct)
    {
        // Request shape is guaranteed by ProduceUploadUrlValidator: SharingWithType is one of
        // Group/Event/Private, and SharedWith is non-null for Group/Event and null for Private.

        // Storage quota is enforced at VIEW/STREAM time, not upload time. Users can always upload
        // regardless of quota status; they manage storage later by deleting old videos if needed.

        var user = User.GetSubject();
        var sharedWith = req.SharedWith;

        if (req.SharingWithType == SharingWithType.Group)
        {
            var canUploadToGroup = await accessService.CanUserUploadToGroupAsync(user, sharedWith!.Value, ct);

            if (!canUploadToGroup)
            {
                logger.LogWarning(
                    "User {0} was trying to add video where he is not assigned. Association EntityId: {1}.", user,
                    sharedWith);
                return await Send.UnauthorizedAsync(ct);
            }
        }
        else if (req.SharingWithType == SharingWithType.Event)
        {
            var canUploadToEvent = await accessService.CanUserUploadToEventAsync(user, sharedWith!.Value, ct);

            if (!canUploadToEvent)
            {
                logger.LogWarning(
                    "User {0} was trying to add video where he is not assigned. Association EntityId: {1}.", user,
                    sharedWith);
                return await Send.UnauthorizedAsync(ct);
            }
        }
        // Private: no group/event access check needed.

        var sharedBlob = await videoService.GetSharingLink(
            user,
            req.NameOfVideo,
            req.FileName,
            req.SharingWithType,
            req.SharedWith,
            ct);

        return await Send.OkAsync(
            new ProduceUploadUrlResponse()
            {
                VideoId = sharedBlob.VideoId, Sas = sharedBlob.Sas.ToString(), ExpireAt = sharedBlob.ExpireAt
            }, ct);
    }
}