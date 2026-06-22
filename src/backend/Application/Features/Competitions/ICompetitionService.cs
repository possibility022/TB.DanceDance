using Domain.Entities;

namespace Application.Features.Competitions;

public interface ICompetitionService
{
    Task<Competition> CreateAsync(
        string userId,
        string name,
        DateTime? date,
        string? location,
        CommentVisibility commentVisibility,
        CancellationToken cancellationToken);

    /// <summary>Renames a competition. Returns false when it doesn't exist or isn't owned by the user.</summary>
    Task<bool> RenameAsync(Guid competitionId, string userId, string newName, CancellationToken cancellationToken);

    /// <summary>Deletes a competition, detaching (not deleting) its videos. Returns false when it doesn't exist or isn't owned by the user.</summary>
    Task<bool> DeleteAsync(Guid competitionId, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds an owned video to an owned competition. Throws <see cref="ArgumentException"/> when the
    /// competition/video doesn't exist, isn't owned by the user, or the video is already in another competition.
    /// </summary>
    Task AddVideoAsync(Guid competitionId, Guid videoId, string userId, CancellationToken cancellationToken);

    /// <summary>Removes a video from a competition (leaves it standalone). Returns false when not found or not owned.</summary>
    Task<bool> RemoveVideoAsync(Guid competitionId, Guid videoId, string userId, CancellationToken cancellationToken);

    /// <summary>Lists the current user's competitions (with their videos), newest first.</summary>
    Task<IReadOnlyCollection<Competition>> ListMyCompetitionsAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Gets a single owned competition with its videos. Returns null when not found or not owned.</summary>
    Task<Competition?> GetAsync(Guid competitionId, string userId, CancellationToken cancellationToken);
}
