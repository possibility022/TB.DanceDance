using Application.Features.Videos.Endpoints.Videos;
using Domain.Entities;
using Domain.Models;
using TB.DanceDance.API.Contracts.Features.Videos;

namespace Application.Features.Videos;

/// <summary>
/// Outcome of an owner-only video delete, mapped by the endpoint to 204 / 403 / 404.
/// </summary>
public enum DeleteVideoResult
{
    /// <summary>No video exists with the given id.</summary>
    NotFound,
    /// <summary>The video exists but the requesting user is not its uploader.</summary>
    Forbidden,
    /// <summary>The video, its related rows and blobs were removed.</summary>
    Deleted,
    /// <summary>The video was transferred to its current owner less than
    /// <see cref="Domain.Entities.VideoTransfer.RollbackWindowDays"/> days ago and is still within
    /// the original sender's rollback window.</summary>
    RollbackPending
}

public interface IVideoService
{
    Task<Stream> OpenStream(string blobName, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the video blob's Content-Type, falling back to "video/webm" when the
    /// stored type is missing or the Azure default (covers blobs uploaded before
    /// Content-Type was set on upload).
    /// </summary>
    Task<string> GetContentType(string blobName, CancellationToken cancellationToken);
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

    /// <summary>
    /// Permanently deletes a video the user owns: the <see cref="Video"/> row (cascading to its
    /// <c>SharedWith</c>, <c>Comment</c>, <c>SharedLink</c> and <c>VideoMetadata</c> rows) and all three
    /// associated blobs (source, converted and thumbnail). Only the uploader may delete.
    /// </summary>
    /// <param name="videoId">The video ID.</param>
    /// <param name="userId">The ID of the user requesting the delete (must be the uploader).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The delete outcome (deleted, forbidden, not found, or rollback-pending).</returns>
    Task<DeleteVideoResult> DeleteVideoAsync(Guid videoId, string userId, CancellationToken cancellationToken);
}