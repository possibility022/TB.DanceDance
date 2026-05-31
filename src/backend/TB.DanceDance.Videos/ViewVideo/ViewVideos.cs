using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Domain.Entities;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.ViewVideo;

/// <summary>
/// Listing queries for videos a user can see. Group/event membership lives in the Access module,
/// so this handler never touches <c>AssignedToGroup</c>/<c>AssignedToEvent</c>: it asks Access for
/// the requesting user's memberships (via Contracts) and then filters the local
/// <c>SharedWith</c>/<c>Video</c> rows accordingly.
/// </summary>
class ViewVideos
    : IRequestHandler<ViewVideosFromGroupQuery, IReadOnlyCollection<VideoDto>>,
      IRequestHandler<ViewVideosFromEventQuery, IReadOnlyCollection<VideoDto>>,
      IRequestHandler<ViewVideosFromAllGroupsQuery, IReadOnlyCollection<VideoDto>>,
      IRequestHandler<ViewPrivateVideosQuery, IReadOnlyCollection<VideoDto>>
{
    private readonly VideosDbContext dbContext;
    private readonly IRequestHandler<GetUserGroupMembershipsQuery, IReadOnlyCollection<GroupMembershipDto>> getUserGroupMembershipsQuery;
    private readonly IRequestHandler<DoesUserHasAccessToSharedWith, bool> doesUserHasAccessToSharedWith;

    public ViewVideos(VideosDbContext dbContext,
        IRequestHandler<GetUserGroupMembershipsQuery,IReadOnlyCollection<GroupMembershipDto>> getUserGroupMembershipsQuery,
        IRequestHandler<DoesUserHasAccessToSharedWith,bool> doesUserHasAccessToSharedWith
        )
    {
        this.dbContext = dbContext;
        this.getUserGroupMembershipsQuery = getUserGroupMembershipsQuery;
        this.doesUserHasAccessToSharedWith = doesUserHasAccessToSharedWith;
    }

    private static readonly Expression<Func<Video, VideoDto>> ToDto = v => new VideoDto()
    {
        Name = v.Name,
        Id = v.Id,
        BlobId = v.BlobId!,
        CommentVisibility = (int)v.CommentVisibility,
        Converted = v.Converted,
        Duration = v.Duration,
        RecordedDateTime = v.RecordedDateTime,
    };

    private static readonly Func<Video, VideoDto> ToDtoCompiled = ToDto.Compile();

    public async Task<IReadOnlyCollection<VideoDto>> HandleAsync(ViewVideosFromGroupQuery request, CancellationToken cancellationToken = default)
    {
        // Membership + join date come from Access — only members see the group's videos, and only
        // those recorded after they joined.
        var memberships = await getUserGroupMembershipsQuery.HandleAsync(new GetUserGroupMembershipsQuery(request.UserId), cancellationToken);
        var membership = memberships.FirstOrDefault(m => m.GroupId == request.GroupId);
        if (membership is null)
            return Array.Empty<VideoDto>();

        var whenJoined = membership.WhenJoined;

        var videos = await dbContext.SharedWith
            .Where(sw => sw.GroupId == request.GroupId && sw.Video.RecordedDateTime > whenJoined)
            .Select(sw => sw.Video)
            .OrderByDescending(v => v.RecordedDateTime)
            .Select(ToDto)
            .ToArrayAsync(cancellationToken);

        return videos.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<VideoDto>> HandleAsync(ViewVideosFromEventQuery request, CancellationToken cancellationToken = default)
    {
        // Event membership has no join-date restriction in the original EventService.GetVideos.
        var isMember = await doesUserHasAccessToSharedWith.HandleAsync(new DoesUserHasAccessToSharedWith
        {
            UserId = request.UserId,
            SharedToId = request.EventId,
            SharedWithType = SharedWithByType.Event,
        }, cancellationToken);

        if (!isMember)
            return Array.Empty<VideoDto>();

        var videos = await dbContext.SharedWith
            .Where(sw => sw.EventId == request.EventId)
            .Select(sw => sw.Video)
            .OrderByDescending(v => v.RecordedDateTime)
            .Select(ToDto)
            .ToArrayAsync(cancellationToken);

        return videos.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<VideoDto>> HandleAsync(ViewVideosFromAllGroupsQuery request, CancellationToken cancellationToken = default)
    {
        var memberships = await getUserGroupMembershipsQuery.HandleAsync(new GetUserGroupMembershipsQuery(request.UserId), cancellationToken);
        if (memberships.Count == 0)
            return Array.Empty<VideoDto>();

        var joinedByGroup = memberships.ToDictionary(m => m.GroupId, m => m.WhenJoined);
        var groupIds = joinedByGroup.Keys.ToArray();

        // The WhenJoined cutoff differs per group and membership lives in another module/schema,
        // so we cannot filter it in SQL — pull the candidate (group, video) rows, then apply the
        // per-group "recorded after joined" rule in memory and dedupe (a video may be shared to
        // several of the user's groups).
        var candidates = await dbContext.SharedWith
            .Where(sw => sw.GroupId != null && groupIds.Contains(sw.GroupId.Value))
            .Select(sw => new { GroupId = sw.GroupId!.Value, sw.Video })
            .ToArrayAsync(cancellationToken);

        var videos = candidates
            .Where(c => c.Video.RecordedDateTime > joinedByGroup[c.GroupId])
            .DistinctBy(c => c.Video.Id)
            .OrderByDescending(c => c.Video.RecordedDateTime)
            .Select(c => ToDtoCompiled(c.Video))
            .ToArray();

        return videos.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<VideoDto>> HandleAsync(ViewPrivateVideosQuery request, CancellationToken cancellationToken = default)
    {
        // Private videos are decided entirely within Videos: shares with no event, no group,
        // owned by the requester. No Access call needed.
        var videos = await dbContext.SharedWith
            .Where(sw => sw.EventId == null && sw.GroupId == null && sw.UserId == request.UserId)
            .Select(sw => sw.Video)
            .OrderByDescending(v => v.RecordedDateTime)
            .Select(ToDto)
            .ToArrayAsync(cancellationToken);

        return videos.AsReadOnly();
    }
}
