using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.Management;

public class RenameVideoHandler : IRequestHandler<RenameVideoCommand,bool>
{
    private readonly VideosDbContext videosDbContext;

    public RenameVideoHandler(VideosDbContext videosDbContext)
    {
        this.videosDbContext = videosDbContext;
    }
    
    public async Task<bool> HandleAsync(RenameVideoCommand request, CancellationToken cancellationToken = default)
    {
        var video = await videosDbContext.Videos.FirstOrDefaultAsync(r => r.Id == request.VideoId, cancellationToken: cancellationToken);

        if (video == null)
            return false;

        video.Name = request.NewName;
        await videosDbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}