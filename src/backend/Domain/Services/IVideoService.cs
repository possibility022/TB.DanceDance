using Domain.Entities;
using Domain.Models;
using TB.DanceDance.API.Contracts.Requests;

namespace Domain.Services;

public interface IVideoService
{
    Task<Stream> OpenStream(string blobName, CancellationToken cancellationToken);
    Task<Video?> GetVideoByBlobAsync(string userId, string blobId, CancellationToken cancellationToken);
    Task<bool> RenameVideoAsync(Guid guid, string newName, CancellationToken cancellationToken);
    Task<UploadContext> GetSharingLink(string userId, string name, string fileName, SharingWithType sharingWithType,
        Guid? sharedWith, CancellationToken cancellationToken);

    Task<UploadContext?> GetSharingLink(Guid videoId,CancellationToken cancellationToken);

    /// <summary>
    /// Updates the comment visibility setting for a video. Only the video owner can update this setting.
    /// </summary>
    /// <param name="videoId">The video ID</param>
    /// <param name="userId">The ID of the user making the update (must be video owner)</param>
    /// <param name="commentVisibility">The new comment visibility setting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if not found or unauthorized</returns>
    Task<bool> UpdateCommentVisibilityAsync(Guid videoId, string userId, CommentVisibility commentVisibility, CancellationToken cancellationToken);
}