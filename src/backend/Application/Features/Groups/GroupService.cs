using Application.Domain.Models;
using Application.Extensions;
using Application.Features.Videos;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Models;
using Group = Domain.Entities.Group;

namespace Application.Features.Groups;

public class GroupService : IGroupService
{
    private readonly IApplicationContext dbContext;
    private readonly IThumbnailUrlService thumbnailUrlService;

    public GroupService(IApplicationContext dbContext, IThumbnailUrlService thumbnailUrlService)
    {
        this.dbContext = dbContext;
        this.thumbnailUrlService = thumbnailUrlService;
    }

    public async Task<ICollection<Group>> GetAllGroups(CancellationToken cancellationToken)
    {
        return await dbContext.Groups.ToListAsync(cancellationToken);
    }

    public Task<bool> IsGroupAdmin(Guid groupId, string userId, CancellationToken cancellationToken)
    {
        return dbContext.GroupsAdmins
            .AnyAsync(ga => ga.GroupId == groupId && ga.UserId == userId, cancellationToken);
    }

    public Task<Guid[]> GetAdministeredGroupIdsAsync(string userId, CancellationToken cancellationToken)
    {
        return dbContext.GroupsAdmins
            .Where(ga => ga.UserId == userId)
            .Select(ga => ga.GroupId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Group> CreateGroupAsync(string name, DateOnly seasonStart, DateOnly seasonEnd, string creatorUserId, CancellationToken cancellationToken)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            SeasonStart = seasonStart,
            SeasonEnd = seasonEnd,
        };

        dbContext.Groups.Add(group);
        dbContext.GroupsAdmins.Add(new GroupAdmin
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = creatorUserId,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return group;
    }

    public Task<GroupAdminModel[]> GetAdminsAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var q = from ga in dbContext.GroupsAdmins
                join u in dbContext.Users on ga.UserId equals u.Id
                where ga.GroupId == groupId
                orderby u.LastName, u.FirstName
                select new GroupAdminModel
                {
                    UserId = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                };

        return q.ToArrayAsync(cancellationToken);
    }

    public async Task<bool> AddAdminAsync(Guid groupId, string userId, CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            return false;

        var alreadyAdmin = await dbContext.GroupsAdmins
            .AnyAsync(ga => ga.GroupId == groupId && ga.UserId == userId, cancellationToken);
        if (alreadyAdmin)
            return true;

        dbContext.GroupsAdmins.Add(new GroupAdmin
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = userId,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RemoveGroupAdminResult> RemoveAdminAsync(Guid groupId, string userId, CancellationToken cancellationToken)
    {
        var admin = await dbContext.GroupsAdmins
            .FirstOrDefaultAsync(ga => ga.GroupId == groupId && ga.UserId == userId, cancellationToken);
        if (admin is null)
            return RemoveGroupAdminResult.NotAnAdmin;

        var adminCount = await dbContext.GroupsAdmins.CountAsync(ga => ga.GroupId == groupId, cancellationToken);
        if (adminCount <= 1)
            return RemoveGroupAdminResult.BlockedLastAdmin;

        dbContext.GroupsAdmins.Remove(admin);
        await dbContext.SaveChangesAsync(cancellationToken);
        return RemoveGroupAdminResult.Removed;
    }

    public Task<GroupMemberModel[]> GetMembersAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var q = from a in dbContext.AssingedToGroups
                join u in dbContext.Users on a.UserId equals u.Id
                where a.GroupId == groupId
                orderby u.LastName, u.FirstName
                select new GroupMemberModel
                {
                    UserId = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    WhenJoined = a.WhenJoined,
                };

        return q.ToArrayAsync(cancellationToken);
    }

    public async Task<bool> UpdateMemberJoinedAsync(Guid groupId, string userId, DateTime whenJoined, CancellationToken cancellationToken)
    {
        var membership = await dbContext.AssingedToGroups
            .FirstOrDefaultAsync(a => a.GroupId == groupId && a.UserId == userId, cancellationToken);
        if (membership is null)
            return false;

        membership.WhenJoined = whenJoined;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveMemberAsync(Guid groupId, string userId, CancellationToken cancellationToken)
    {
        var membership = await dbContext.AssingedToGroups
            .FirstOrDefaultAsync(a => a.GroupId == groupId && a.UserId == userId, cancellationToken);
        if (membership is null)
            return false;

        dbContext.AssingedToGroups.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    [Obsolete]
    public Task<VideoFromGroupInfo[]> GetUserVideosForGroup(string userId, Guid groupId, CancellationToken cancellationToken)
    {
        var q = from danceGroup in dbContext.Groups
                join assignedTo in dbContext.AssingedToGroups on danceGroup.Id equals assignedTo.GroupId
                join sharedWith in dbContext.SharedWith on assignedTo.GroupId equals sharedWith.GroupId
                join video in dbContext.Videos on sharedWith.VideoId equals video.Id
                where assignedTo.UserId == userId && assignedTo.WhenJoined < video.RecordedDateTime && danceGroup.Id == groupId
                orderby video.RecordedDateTime descending
                select new VideoFromGroupInfo()
                {
                    GroupId = danceGroup.Id,
                    GroupName = danceGroup.Name,
                    Video = video,
                    Group = danceGroup
                };

        return q.ToArrayAsync(cancellationToken);
    }

    [Obsolete]
    public Task<VideoFromGroupInfo[]> GetUserVideosForAllGroups(string userId, CancellationToken cancellationToken)
    {
        var q = from assignedTo in dbContext.AssingedToGroups
                join sharedWith in dbContext.SharedWith on assignedTo.GroupId equals sharedWith.GroupId
                join danceGroup in dbContext.Groups on assignedTo.GroupId equals danceGroup.Id
                join video in dbContext.Videos on sharedWith.VideoId equals video.Id
                where assignedTo.UserId == userId && assignedTo.WhenJoined < video.RecordedDateTime
                orderby video.RecordedDateTime descending
                select new VideoFromGroupInfo()
                {
                    GroupId = danceGroup.Id,
                    GroupName = danceGroup.Name,
                    Video = video,
                    Group = danceGroup
                };

        return q.ToArrayAsync(cancellationToken);
    }

    public async Task<(IReadOnlyCollection<VideoFromGroupInformation> Items, int TotalCount)> GetAllVideos(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var q = from assignedTo in dbContext.AssingedToGroups
            join sharedWith in dbContext.SharedWith on assignedTo.GroupId equals sharedWith.GroupId
            join danceGroup in dbContext.Groups on assignedTo.GroupId equals danceGroup.Id
            join video in dbContext.Videos on sharedWith.VideoId equals video.Id
            where assignedTo.UserId == userId && assignedTo.WhenJoined < video.RecordedDateTime
            orderby video.RecordedDateTime descending
            select new
            {
                Information = new VideoFromGroupInformation()
                {
                    GroupId = danceGroup.Id,
                    GroupName = danceGroup.Name,
                    BlobId = video.BlobId ?? string.Empty,
                    CommentVisibility = (int)video.CommentVisibility,
                    Duration = video.Duration,
                    Name = video.Name,
                    RecordedDateTime = video.RecordedDateTime,
                    Converted = video.Converted,
                    VideoId = video.Id,
                    IsOwner = video.OwnerUserId == userId,
                },
                video.ThumbnailBlobId
            };

        var (rows, totalCount) = await q.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        var items = rows.Select(r =>
        {
            r.Information.ThumbnailUrl = thumbnailUrlService.GetThumbnailUrl(r.ThumbnailBlobId);
            return r.Information;
        }).ToArray();

        return (items, totalCount);
    }

    public async Task<(IReadOnlyCollection<VideoFromGroupInformation> Items, int TotalCount)> GetAllVideos(string userId, Guid groupId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var q = from assignedTo in dbContext.AssingedToGroups
            join sharedWith in dbContext.SharedWith on assignedTo.GroupId equals sharedWith.GroupId
            join danceGroup in dbContext.Groups on assignedTo.GroupId equals danceGroup.Id
            join video in dbContext.Videos on sharedWith.VideoId equals video.Id
            where assignedTo.UserId == userId && assignedTo.WhenJoined < video.RecordedDateTime && assignedTo.GroupId == groupId
            orderby video.RecordedDateTime descending
            select new
            {
                Information = new VideoFromGroupInformation()
                {
                    GroupId = danceGroup.Id,
                    GroupName = danceGroup.Name,
                    BlobId = video.BlobId ?? string.Empty,
                    CommentVisibility = (int)video.CommentVisibility,
                    Duration = video.Duration,
                    Name = video.Name,
                    RecordedDateTime = video.RecordedDateTime,
                    Converted = video.Converted,
                    VideoId = video.Id,
                    IsOwner = video.OwnerUserId == userId,
                },
                video.ThumbnailBlobId
            };

        var (rows, totalCount) = await q.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        var items = rows.Select(r =>
        {
            r.Information.ThumbnailUrl = thumbnailUrlService.GetThumbnailUrl(r.ThumbnailBlobId);
            return r.Information;
        }).ToArray();

        return (items, totalCount);
    }
}
