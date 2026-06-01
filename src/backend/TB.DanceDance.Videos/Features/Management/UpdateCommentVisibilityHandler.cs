using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Domain;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.Features.Management;

public class UpdateCommentVisibilityHandler : IRequestHandler<UpdateCommentVisibilityCommand, bool>
{
    private readonly VideosDbContext videosDbContext;

    public UpdateCommentVisibilityHandler(VideosDbContext videosDbContext)
    {
        this.videosDbContext = videosDbContext;
    }

    public async Task<bool> HandleAsync(UpdateCommentVisibilityCommand request, CancellationToken cancellationToken = default)
    {
        var video = await videosDbContext.Videos
            .FirstOrDefaultAsync(v => v.Id == request.VideoId, cancellationToken);

        if (video == null)
        {
            return false;
        }

        // Only the video owner can update comment visibility
        if (video.UploadedBy != request.UserId)
        {
            return false;
        }

        video.CommentVisibility = (CommentVisibility)request.CommentVisibility;
        await videosDbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
