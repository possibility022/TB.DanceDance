using Application.Extensions;
using Application.Features.AccessManagement;
using Application.Features.Videos.Models;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using TB.DanceDance.API.Contracts.Features.Videos;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Videos;

public record ProduceUploadUrlRequest
{
        [Required]
        [MaxLength(100)]
        [MinLength(5)]
        public string NameOfVideo { get; set; } = string.Empty;

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public DateTime RecordedTimeUtc { get; set; } = DateTime.MinValue;

        /// <summary>
        /// The Group or Event ID to share with. Required when SharingWithType is Group or Event.
        /// Must be null when SharingWithType is Private.
        /// </summary>
        public Guid? SharedWith { get; set; }

        [Required]
        public SharingWithType SharingWithType { get; set; }
        
        /// <summary>
        /// VideoId of an existing upload to resume. Provide this when you want to get a SAS URL for a previously created blob that was partially
        /// uploaded, and you want to continue the upload.
        /// </summary>
        public Guid? VideoId { get; set; }
}

public enum SharingWithType
{
    NotSpecified,
    Group,
    Event,
    Private
}

public record ProduceUploadUrlResponse : UploadUrlResponse
{
    
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
        string? user = null;
        var sharedWith = req.SharedWith;

        // TODO: Storage quota is enforced at VIEW/STREAM time, not upload time
        // Users can always upload videos regardless of quota status
        // Quota enforcement happens when trying to view/stream the video
        // This allows users to upload first, then manage storage by deleting old videos if needed

        user = User.GetSubject();

        // Handle different sharing types
        if (req.SharingWithType == SharingWithType.Group)
        {
            if (sharedWith == null)
            {
                // todo, move to validation
                // ModelState.AddModelError(nameof(sharedVideoInformation.SharedWith), "SharedWith is required for Group sharing type.");
                // return BadRequest(ModelState);
            }

            var canUploadToGroup = await accessService.CanUserUploadToGroupAsync(user, sharedWith.Value, ct);

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
            if (sharedWith == null)
            {
                // todo, move to validation
                // ModelState.AddModelError(nameof(sharedVideoInformation.SharedWith), "SharedWith is required for Event sharing type.");
                // return BadRequest(ModelState);
            }

            var canUploadToEvent = await accessService.CanUserUploadToEventAsync(user, sharedWith!.Value, ct);

            if (!canUploadToEvent)
            {
                logger.LogWarning(
                    "User {0} was trying to add video where he is not assigned. Association EntityId: {1}.", user,
                    sharedWith);
                return await Send.UnauthorizedAsync(ct);
            }
        }
        else if (req.SharingWithType == SharingWithType.Private)
        {
            // Private videos: no group/event access check needed
            // SharedWith should be null for private videos
            if (sharedWith != null)
            {
                // todo, move to validation
                // ModelState.AddModelError(nameof(sharedVideoInformation.SharedWith), "SharedWith must be null for Private sharing type.");
                // return BadRequest(ModelState);
            }
        }
        else
        {
            logger.LogWarning("Invalid sharing type: {0}", req.SharingWithType);
            return await Send.ErrorsAsync(cancellation: ct);
        }
        
        // todo, move to validation
        // if (!ModelState.IsValid)
        // {
        //     return BadRequest(ModelState);
        // }

        var sharedBlob = await videoService.GetSharingLink(
            user,
            req.NameOfVideo,
            req.FileName,
            //req.SharingWithType, 
            TB.DanceDance.API.Contracts.Features.Videos.SharingWithType.NotSpecified, // todo
            req.SharedWith,
            ct);

        return await Send.OkAsync(
            new ProduceUploadUrlResponse()
            {
                VideoId = sharedBlob.VideoId, Sas = sharedBlob.Sas.ToString(), ExpireAt = sharedBlob.ExpireAt
            }, ct);
    }
}