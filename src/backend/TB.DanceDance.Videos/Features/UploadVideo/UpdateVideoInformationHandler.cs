using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Domain.Entities;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.Features.UploadVideo;

internal class UpdateVideoInformationHandler : IRequestHandler<UpdateVideoInformationCommand, bool>
{
    private readonly VideosDbContext dbContext;

    public UpdateVideoInformationHandler(VideosDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<bool> HandleAsync(UpdateVideoInformationCommand request, CancellationToken cancellationToken = default)
    {
        var video = await dbContext.Videos.FirstOrDefaultAsync(r => r.Id == request.VideoId, cancellationToken);

        if (video == null)
            return false;

        video.Duration = request.Duration;
        video.RecordedDateTime = DateTime.SpecifyKind(request.Recorded, DateTimeKind.Utc);

        if (request.Metadata != null)
        {
            dbContext.VideoMetadata.Add(VideoMetadata.Factory.Create(video.Id, request.Metadata));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
