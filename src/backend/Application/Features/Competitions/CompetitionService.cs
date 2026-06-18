using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Competitions;

public class CompetitionService : ICompetitionService
{
    private readonly IApplicationContext dbContext;

    public CompetitionService(IApplicationContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Competition> CreateAsync(
        string userId,
        string name,
        DateTime? date,
        string? location,
        CommentVisibility commentVisibility,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Competition name is required.", nameof(name));
        }

        var competition = new Competition
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            OwnerUserId = userId,
            // timestamp with time zone columns require UTC (matches the Event create flow).
            Date = date?.ToUniversalTime(),
            Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim(),
            CommentVisibility = commentVisibility,
            CreatedDateTime = DateTime.UtcNow
        };

        dbContext.Competitions.Add(competition);
        await dbContext.SaveChangesAsync(cancellationToken);

        return competition;
    }

    public async Task<bool> RenameAsync(Guid competitionId, string userId, string newName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Competition name is required.", nameof(newName));
        }

        var competition = await dbContext.Competitions
            .FirstOrDefaultAsync(c => c.Id == competitionId, cancellationToken);

        if (competition == null || competition.OwnerUserId != userId)
        {
            return false;
        }

        competition.Name = newName.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid competitionId, string userId, CancellationToken cancellationToken)
    {
        var competition = await dbContext.Competitions
            .Include(c => c.Videos)
            .FirstOrDefaultAsync(c => c.Id == competitionId, cancellationToken);

        if (competition == null || competition.OwnerUserId != userId)
        {
            return false;
        }

        // Detach videos rather than cascade-deleting them: they become standalone again.
        foreach (var video in competition.Videos)
        {
            video.CompetitionId = null;
        }

        dbContext.Competitions.Remove(competition);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task AddVideoAsync(Guid competitionId, Guid videoId, string userId, CancellationToken cancellationToken)
    {
        var competition = await dbContext.Competitions
            .FirstOrDefaultAsync(c => c.Id == competitionId, cancellationToken);

        if (competition == null || competition.OwnerUserId != userId)
        {
            throw new ArgumentException($"Competition {competitionId} was not found.", nameof(competitionId));
        }

        var video = await dbContext.Videos
            .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken);

        if (video == null)
        {
            throw new ArgumentException($"Video {videoId} was not found.", nameof(videoId));
        }

        // Only the owner of the video may group it.
        if (video.OwnerUserId != userId)
        {
            throw new ArgumentException($"User {userId} does not own video {videoId}.", nameof(videoId));
        }

        // Already in this competition: idempotent no-op.
        if (video.CompetitionId == competitionId)
        {
            return;
        }

        // A video belongs to at most one competition.
        if (video.CompetitionId != null)
        {
            throw new ArgumentException("This video is already in another competition.", nameof(videoId));
        }

        video.CompetitionId = competitionId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveVideoAsync(Guid competitionId, Guid videoId, string userId, CancellationToken cancellationToken)
    {
        var competition = await dbContext.Competitions
            .FirstOrDefaultAsync(c => c.Id == competitionId, cancellationToken);

        if (competition == null || competition.OwnerUserId != userId)
        {
            return false;
        }

        var video = await dbContext.Videos
            .FirstOrDefaultAsync(v => v.Id == videoId && v.CompetitionId == competitionId, cancellationToken);

        if (video == null)
        {
            return false;
        }

        video.CompetitionId = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<Competition>> ListMyCompetitionsAsync(string userId, CancellationToken cancellationToken)
    {
        var competitions = await dbContext.Competitions
            .Include(c => c.Videos)
            .Where(c => c.OwnerUserId == userId)
            .OrderByDescending(c => c.CreatedDateTime)
            .ToListAsync(cancellationToken);

        return competitions.AsReadOnly();
    }

    public async Task<Competition?> GetAsync(Guid competitionId, string userId, CancellationToken cancellationToken)
    {
        var competition = await dbContext.Competitions
            .Include(c => c.Videos)
            .FirstOrDefaultAsync(c => c.Id == competitionId, cancellationToken);

        if (competition == null || competition.OwnerUserId != userId)
        {
            return null;
        }

        return competition;
    }
}
