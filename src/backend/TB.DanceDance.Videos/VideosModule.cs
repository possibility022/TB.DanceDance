using TB.DanceDance.Utilities.Infrastructure.Models;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
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
            .Register<SharedWithByVideoBlobIdQuery, IReadOnlyCollection<SharedWithResponse>, SharedWithByVideoHandlers>()
            .Register<SharedWithByVideoIdQuery, IReadOnlyCollection<SharedWithResponse>, SharedWithByVideoHandlers>()
            .Register<DoesUserHaveAccessToVideoQuery, bool, DoesUserHaveAccessToVideoHandler>()
            .Register<DoesUserHaveAccessToVideoByBlobQuery, bool, DoesUserHaveAccessToVideoHandler>()
            .Register<GetVideoForViewingQuery, VideoDto?, GetVideoForViewingHandler>()
            .Register<OpenVideoStreamQuery, Stream, OpenVideoStreamHandler>();
    }
}
