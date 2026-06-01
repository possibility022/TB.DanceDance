using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.API.Contracts.Features.Videos;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Mappers;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using ApiSharingWithType = TB.DanceDance.API.Contracts.Features.Videos.SharingWithType;
using VideosSharingWithType = TB.DanceDance.Videos.Contracts.SharingWithType;

namespace TB.DanceDance.API.Features.Videos;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
public class VideoController : Controller
{
    private readonly IRequestHandler<OpenVideoStreamQuery, Stream> openVideoStreamQuery;
    private readonly IRequestHandler<ViewPrivateVideosQuery, IReadOnlyCollection<VideoDto>> viewPrivateVideosQuery;
    private readonly IRequestHandler<GetVideoForViewingQuery, VideoDto?> getVideoForViewingQuery;
    private readonly IRequestHandler<DoesUserHaveAccessToVideoQuery, bool> doesUserHaveAccessToVideoQuery;
    private readonly IRequestHandler<RenameVideoCommand, bool> renameVideoCommand;
    private readonly IRequestHandler<DoesUserHaveAccessToVideoByBlobQuery, bool> doesUserHaveAccessToVideoByBlobQuery;
    private readonly IRequestHandler<CreateSharingLinkCommand, UploadContext?> createSharingLinkCommand;
    private readonly IRequestHandler<CanUserUploadToGroupRequest, bool> canUserUploadToGroupRequest;
    private readonly IRequestHandler<CanUserUploadToEventRequest, bool> canUserUploadToEventRequest;
    private readonly IRequestHandler<CreateVideoUploadCommand, UploadContext?> createVideoUploadCommand;
    private readonly IRequestHandler<UpdateCommentVisibilityCommand, bool> updateCommentVisibilityCommand;
    private readonly ILogger<VideoController> logger;

    public VideoController(
        IRequestHandler<OpenVideoStreamQuery, Stream> openVideoStreamQuery,
        IRequestHandler<ViewPrivateVideosQuery, IReadOnlyCollection<VideoDto>> viewPrivateVideosQuery,
        IRequestHandler<GetVideoForViewingQuery, VideoDto?> getVideoForViewingQuery,
        IRequestHandler<DoesUserHaveAccessToVideoQuery, bool> doesUserHaveAccessToVideoQuery,
        IRequestHandler<RenameVideoCommand, bool> renameVideoCommand,
        IRequestHandler<DoesUserHaveAccessToVideoByBlobQuery, bool> doesUserHaveAccessToVideoByBlobQuery,
        IRequestHandler<CreateSharingLinkCommand, UploadContext?> createSharingLinkCommand,
        IRequestHandler<CanUserUploadToGroupRequest, bool> canUserUploadToGroupRequest,
        IRequestHandler<CanUserUploadToEventRequest, bool> canUserUploadToEventRequest,
        IRequestHandler<CreateVideoUploadCommand, UploadContext?> createVideoUploadCommand,
        IRequestHandler<UpdateCommentVisibilityCommand, bool> updateCommentVisibilityCommand,
        ILogger<VideoController> logger)
    {
        this.openVideoStreamQuery = openVideoStreamQuery;
        this.viewPrivateVideosQuery = viewPrivateVideosQuery;
        this.getVideoForViewingQuery = getVideoForViewingQuery;
        this.doesUserHaveAccessToVideoQuery = doesUserHaveAccessToVideoQuery;
        this.renameVideoCommand = renameVideoCommand;
        this.doesUserHaveAccessToVideoByBlobQuery = doesUserHaveAccessToVideoByBlobQuery;
        this.createSharingLinkCommand = createSharingLinkCommand;
        this.canUserUploadToGroupRequest = canUserUploadToGroupRequest;
        this.canUserUploadToEventRequest = canUserUploadToEventRequest;
        this.createVideoUploadCommand = createVideoUploadCommand;
        this.updateCommentVisibilityCommand = updateCommentVisibilityCommand;
        this.logger = logger;
    }

    [Route(VideoRoutes.MyVideos)]
    public async Task<IActionResult> MyVideos(CancellationToken cancellationToken)
    {
        var videos = await viewPrivateVideosQuery.HandleAsync(new ViewPrivateVideosQuery(User.GetSubject()), cancellationToken);
        var apiModel = videos.Select(ContractMappers.MapToVideoInformation);

        return new OkObjectResult(apiModel);
    }

    [Route(VideoRoutes.GetSingle)]
    [HttpGet]
    public async Task<IActionResult> GetInformationAsync(string guid, CancellationToken cancellationToken)
    {
        string user = User.GetSubject();

        var info = await getVideoForViewingQuery.HandleAsync(new GetVideoForViewingQuery(user, guid), cancellationToken);

        if (info == null)
            return NotFound();

        var results = ContractMappers.MapToVideoInformation(info);

        return new OkObjectResult(results);
    }

    [Route(VideoRoutes.GetStream)]
    [HttpGet]
    public async Task<IActionResult> GetStreamAsync(string guid, CancellationToken cancellationToken)
    {
        var userSubjectId = User.TryGetSubject();
        if (string.IsNullOrWhiteSpace(userSubjectId))
            return Unauthorized();

        // TODO: Implement storage quota enforcement for private videos at VIEW/STREAM time
        // Check if this is a private video (SharedWith.EventId == null && SharedWith.GroupId == null)
        // If private:
        //   1. Calculate user's total private video ConvertedBlobSize
        //   2. Compare against User.StorageQuotaBytes
        //   3. If over quota, return 403 Forbidden with message about storage limit
        // This allows users to upload videos even over quota, but they cannot view them until space is freed

        var hasAccess = await doesUserHaveAccessToVideoByBlobQuery.HandleAsync(new DoesUserHaveAccessToVideoByBlobQuery(userSubjectId, guid), cancellationToken);
        if (!hasAccess)
            return new UnauthorizedResult();

        var stream = await openVideoStreamQuery.HandleAsync(new OpenVideoStreamQuery(guid), cancellationToken);
        return File(stream, "video/mp4", enableRangeProcessing: true);
    }

    [Route(VideoRoutes.Rename)]
    [HttpPost]
    public async Task<IActionResult> RenameVideo([FromRoute] Guid videoId, [FromBody] VideoRenameRequest input, CancellationToken cancellationToken)
    {
        var hasAccess = await doesUserHaveAccessToVideoQuery.HandleAsync(new DoesUserHaveAccessToVideoQuery(User.GetSubject(), videoId), cancellationToken);
        if (!hasAccess)
            return Unauthorized();

        var res = await renameVideoCommand.HandleAsync(new RenameVideoCommand { VideoId = videoId, NewName = input.NewName }, cancellationToken);

        if (res == false)
            return BadRequest();

        return Ok();
    }

    [HttpGet]
    [Route(VideoRoutes.RefreshUploadUrl)]
    public async Task<ActionResult<UploadVideoInformationResponse>> GetUploadInformation([FromRoute] Guid videoId, CancellationToken cancellationToken)
    {
        string user = User.GetSubject();
        var hasAccess = await doesUserHaveAccessToVideoQuery.HandleAsync(new DoesUserHaveAccessToVideoQuery(user, videoId), cancellationToken);
        if (!hasAccess)
            return Unauthorized();

        var sharedBlob = await createSharingLinkCommand.HandleAsync(new CreateSharingLinkCommand { VideoId = videoId }, cancellationToken);

        if (sharedBlob == null)
            return NotFound();

        return new UploadVideoInformationResponse()
        {
            Sas = sharedBlob.Sas.ToString(), VideoId = sharedBlob.VideoId, ExpireAt = sharedBlob.ExpireAt
        };
    }

    [Route(VideoRoutes.GetUploadUrl)]
    [HttpPost]
    public async Task<ActionResult<UploadVideoInformationResponse>> GetUploadInformation(
        [FromBody] SharedVideoInformationRequest sharedVideoInformation,
        CancellationToken cancellationToken
        )
    {
        if (sharedVideoInformation == null)
        {
            return BadRequest("SharedVideoInformation is required.");
        }

        var sharedWith = sharedVideoInformation.SharedWith;

        // TODO: Storage quota is enforced at VIEW/STREAM time, not upload time
        // Users can always upload videos regardless of quota status
        // Quota enforcement happens when trying to view/stream the video
        // This allows users to upload first, then manage storage by deleting old videos if needed

        var user = User.GetSubject();

        // Handle different sharing types
        if (sharedVideoInformation.SharingWithType == ApiSharingWithType.Group)
        {
            if (sharedWith == null)
            {
                ModelState.AddModelError(nameof(sharedVideoInformation.SharedWith), "SharedWith is required for Group sharing type.");
                return BadRequest(ModelState);
            }

            var canUploadToGroup = await canUserUploadToGroupRequest.HandleAsync(new CanUserUploadToGroupRequest { UserId = user, GroupId = sharedWith.Value }, cancellationToken);

            if (!canUploadToGroup)
            {
                logger.LogWarning(
                    "User {0} was trying to add video where he is not assigned. Association EntityId: {1}.", user,
                    sharedWith);
                return new UnauthorizedResult();
            }
        }
        else if (sharedVideoInformation.SharingWithType == ApiSharingWithType.Event)
        {
            if (sharedWith == null)
            {
                ModelState.AddModelError(nameof(sharedVideoInformation.SharedWith), "SharedWith is required for Event sharing type.");
                return BadRequest(ModelState);
            }

            var canUploadToEvent = await canUserUploadToEventRequest.HandleAsync(new CanUserUploadToEventRequest { UserId = user, EventId = sharedWith.Value }, cancellationToken);

            if (!canUploadToEvent)
            {
                logger.LogWarning(
                    "User {0} was trying to add video where he is not assigned. Association EntityId: {1}.", user,
                    sharedWith);
                return new UnauthorizedResult();
            }
        }
        else if (sharedVideoInformation.SharingWithType == ApiSharingWithType.Private)
        {
            // Private videos: no group/event access check needed
            // SharedWith should be null for private videos
            if (sharedWith != null)
            {
                ModelState.AddModelError(nameof(sharedVideoInformation.SharedWith), "SharedWith must be null for Private sharing type.");
                return BadRequest(ModelState);
            }
        }
        else
        {
            return new BadRequestResult();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var sharedBlob = await createVideoUploadCommand.HandleAsync(new CreateVideoUploadCommand
        {
            UserId = user,
            Name = sharedVideoInformation.NameOfVideo,
            FileName = sharedVideoInformation.FileName,
            SharingWithType = MapSharingWithType(sharedVideoInformation.SharingWithType),
            SharedWith = sharedVideoInformation.SharedWith,
        }, cancellationToken);

        if (sharedBlob == null)
            return NotFound();

        return new UploadVideoInformationResponse()
        {
            Sas = sharedBlob.Sas.ToString(), VideoId = sharedBlob.VideoId, ExpireAt = sharedBlob.ExpireAt
        };
    }

    /// <summary>
    /// Updates the comment visibility setting for a video. Only the video owner can update this.
    /// </summary>
    [HttpPut]
    [Route(VideoRoutes.UpdateCommentSettings)]
    public async Task<IActionResult> UpdateCommentSettings(
        [FromRoute] Guid videoId,
        [FromBody] UpdateVideoCommentSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        var result = await updateCommentVisibilityCommand.HandleAsync(new UpdateCommentVisibilityCommand
        {
            VideoId = videoId,
            UserId = userId,
            CommentVisibility = request.CommentVisibility,
        }, cancellationToken);

        if (!result)
        {
            return NotFound(new { error = "Video not found or you are not authorized to update its settings." });
        }

        return Ok();
    }

    private static VideosSharingWithType MapSharingWithType(ApiSharingWithType type) => type switch
    {
        ApiSharingWithType.Group => VideosSharingWithType.Group,
        ApiSharingWithType.Event => VideosSharingWithType.Event,
        ApiSharingWithType.Private => VideosSharingWithType.Private,
        _ => VideosSharingWithType.NotSpecified,
    };
}
