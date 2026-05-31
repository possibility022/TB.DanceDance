using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TB.DanceDance.Utilities.Infrastructure.Models;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Comments;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Infrastructure;
using TB.DanceDance.Videos.Management;
using TB.DanceDance.Videos.Sharing;
using TB.DanceDance.Videos.UploadVideo;
using TB.DanceDance.Videos.ViewVideo;

namespace TB.DanceDance.Videos;

public static class VideosModule
{
    public static MediatorBuilder AddVideosModule(this MediatorBuilder builder)
    {
        return builder
            .Register<GetNextVideoToConvertQuery, VideoToConvertDto?, GetNextVideoToConvertHandler>()
            .Register<UpdateVideoInformationCommand, bool, UpdateVideoInformationHandler>()
            .Register<UploadConvertedVideoCommand, Guid?, UploadConvertedVideoHandler>()
            .Register<GetPublishSasQuery, SharedBlob?, GetPublishSasHandler>()
            .Register<RenameVideoCommand, bool, RenameVideoHandler>()
            .Register<CreateSharingLinkCommand, UploadContext?, CreateSharingLinkCommandHandler>()
            .Register<CreateVideoUploadCommand, UploadContext?, CreateVideoUploadHandler>()
            .Register<UpdateCommentVisibilityCommand, bool, UpdateCommentVisibilityHandler>()
            .Register<SharedWithByVideoBlobIdQuery, IReadOnlyCollection<SharedWithResponse>, SharedWithByVideoHandlers>()
            .Register<SharedWithByVideoIdQuery, IReadOnlyCollection<SharedWithResponse>, SharedWithByVideoHandlers>()
            .Register<DoesUserHaveAccessToVideoQuery, bool, DoesUserHaveAccessToVideoHandler>()
            .Register<DoesUserHaveAccessToVideoByBlobQuery, bool, DoesUserHaveAccessToVideoHandler>()
            .Register<GetVideoForViewingQuery, VideoDto?, GetVideoForViewingHandler>()
            .Register<OpenVideoStreamQuery, Stream, OpenVideoStreamHandler>()
            .Register<ViewVideosFromGroupQuery, IReadOnlyCollection<VideoDto>, ViewVideos>()
            .Register<ViewVideosFromEventQuery, IReadOnlyCollection<VideoDto>, ViewVideos>()
            .Register<ViewVideosFromAllGroupsQuery, IReadOnlyCollection<VideoDto>, ViewVideos>()
            .Register<ViewPrivateVideosQuery, IReadOnlyCollection<VideoDto>, ViewVideos>()
            .Register<CreateSharedLinkCommand, SharedLinkDto, SharedLinkHandlers>()
            .Register<GetVideoBySharedLinkQuery, VideoDto?, SharedLinkHandlers>()
            .Register<RevokeSharedLinkCommand, bool, SharedLinkHandlers>()
            .Register<GetUserSharedLinksQuery, IReadOnlyCollection<SharedLinkDto>, SharedLinkHandlers>()
            .Register<GetSharedLinkQuery, SharedLinkDto?, SharedLinkHandlers>()
            .Register<CreateCommentCommand, CommentDto, CommentHandlers>()
            .Register<GetCommentsForVideoByLinkQuery, IReadOnlyCollection<CommentDto>, CommentHandlers>()
            .Register<GetCommentsForVideoByIdQuery, IReadOnlyCollection<CommentDto>, CommentHandlers>()
            .Register<UpdateCommentCommand, bool, CommentHandlers>()
            .Register<DeleteCommentCommand, bool, CommentHandlers>()
            .Register<HideCommentCommand, bool, CommentHandlers>()
            .Register<UnhideCommentCommand, bool, CommentHandlers>()
            .Register<ReportCommentCommand, bool, CommentHandlers>();
    }

    /// <summary>
    /// Registers the Videos module's <see cref="VideosDbContext"/> against the shared PostgreSQL
    /// database. One physical database; the context maps to the <c>video</c> and <c>comments</c>
    /// schemas.
    /// </summary>
    public static IServiceCollection AddVideosModuleInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<VideosDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
