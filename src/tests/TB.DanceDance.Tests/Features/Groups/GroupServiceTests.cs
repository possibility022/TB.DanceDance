using Application.Features.Groups;
using Application.Features.Videos;
using Domain.Entities;
using Infrastructure.Data;
using NSubstitute;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Features.Groups;

public class GroupServiceTests : BaseTestClass
{
    private readonly IThumbnailUrlService thumbnailUrlService;
    private GroupService groupService = null!;

    public GroupServiceTests(DanceDbFixture dbContextFixture) : base(dbContextFixture)
    {
        thumbnailUrlService = Substitute.For<IThumbnailUrlService>();
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        groupService = new GroupService(runtimeDbContext, thumbnailUrlService);
        return ValueTask.CompletedTask;
    }

    // G1: Returns videos for specific group when user is a member
    [Fact]
    public async Task GetUserVideosForGroup_ReturnsOnlyGroupVideos_ForMember()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        var v = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(1)).Build();
        var share = new SharedWith { Id = Guid.NewGuid(), VideoId = v.Id, UserId = user.Id, GroupId = group.Id };

        SeedDbContext.AddRange(user, group, membership, v, share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await groupService.GetUserVideosForGroup(user.Id, group.Id, TestContext.Current.CancellationToken);
        Assert.Single(result);
        Assert.Equal(group.Id, result.Single().GroupId);
        Assert.Equal(group.Name, result.Single().GroupName);
        Assert.Equal(v.Id, result.Single().Video.Id);
    }

    // G2: Excludes videos recorded before user joined the group
    [Fact]
    public async Task GetUserVideosForGroup_ExcludesVideosRecordedBeforeJoin()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        var v = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(-5)).Build();
        var share = new SharedWith { Id = Guid.NewGuid(), VideoId = v.Id, UserId = user.Id, GroupId = group.Id };

        SeedDbContext.AddRange(user, group, membership, v, share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await groupService.GetUserVideosForGroup(user.Id, group.Id, TestContext.Current.CancellationToken);
        Assert.Empty(result);
    }

    // G3: Excludes videos shared to other groups
    [Fact]
    public async Task GetUserVideosForGroup_ExcludesOtherGroups()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var groupA = new GroupDataBuilder().Build();
        var groupB = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membershipA = userB.AssignTo(groupA, joinedAt);

        var vB = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(1)).Build();
        var shareB = new SharedWith { Id = Guid.NewGuid(), VideoId = vB.Id, UserId = user.Id, GroupId = groupB.Id };

        SeedDbContext.AddRange(user, groupA, groupB, membershipA, vB, shareB);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await groupService.GetUserVideosForGroup(user.Id, groupA.Id, TestContext.Current.CancellationToken);
        Assert.Empty(result);
    }

    // G4: Direct shares (no GroupId) are excluded
    [Fact]
    public async Task GetUserVideosForGroup_ExcludesDirectShares()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        var v = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(1)).Build();
        var directShare = new SharedWith { Id = Guid.NewGuid(), VideoId = v.Id, UserId = user.Id };

        SeedDbContext.AddRange(user, group, membership, v, directShare);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await groupService.GetUserVideosForGroup(user.Id, group.Id, TestContext.Current.CancellationToken);
        Assert.Empty(result);
    }

    // G5: Multiple videos sorted by RecordedDateTime descending
    [Fact]
    public async Task GetUserVideosForGroup_SortsByRecordedDateTime_Descending()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        var v1 = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddHours(1)).Build();
        var v2 = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddHours(3)).Build();
        var v3 = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddHours(2)).Build();

        SeedDbContext.AddRange(user, group, membership, v1, v2, v3);
        SeedDbContext.AddRange(
            new SharedWith { Id = Guid.NewGuid(), VideoId = v1.Id, UserId = user.Id, GroupId = group.Id },
            new SharedWith { Id = Guid.NewGuid(), VideoId = v2.Id, UserId = user.Id, GroupId = group.Id },
            new SharedWith { Id = Guid.NewGuid(), VideoId = v3.Id, UserId = user.Id, GroupId = group.Id }
        );
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await groupService.GetUserVideosForGroup(user.Id, group.Id, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
        Assert.Equal(new[] { v2.Id, v3.Id, v1.Id }, result.Select(r => r.Video.Id).ToArray());
    }

    // G6: Duplicate shares produce duplicate results (document current behavior)
    [Fact]
    public async Task GetUserVideosForGroup_ReturnsDuplicates_WhenDuplicateSharesExist()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);
        var v = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(1)).Build();

        SeedDbContext.AddRange(user, group, membership, v);
        SeedDbContext.AddRange(
            new SharedWith { Id = Guid.NewGuid(), VideoId = v.Id, UserId = user.Id, GroupId = group.Id },
            new SharedWith { Id = Guid.NewGuid(), VideoId = v.Id, UserId = user.Id, GroupId = group.Id }
        );
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await groupService.GetUserVideosForGroup(user.Id, group.Id, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
        Assert.Equal(2, result.Length);
        Assert.All(result, r => Assert.Equal(v.Id, r.Video.Id));
    }

    // G7: No membership -> no videos
    [Fact]
    public async Task GetUserVideosForGroup_ReturnsEmpty_WhenUserNotMember()
    {
        var user = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        var v = new VideoDataBuilder().UploadedBy(user).RecordedAt(DateTime.UtcNow.AddMinutes(1)).Build();
        var share = new SharedWith { Id = Guid.NewGuid(), VideoId = v.Id, UserId = user.Id, GroupId = group.Id };

        SeedDbContext.AddRange(user, group, v, share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await groupService.GetUserVideosForGroup(user.Id, group.Id, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
        Assert.Empty(result);
    }

    // G8: Boundary condition: RecordedDateTime equal to WhenJoined is excluded (strict <)
    [Fact]
    public async Task GetUserVideosForGroup_Excludes_WhenRecordedEqualsJoin()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        var v = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt).Build();
        var share = new SharedWith { Id = Guid.NewGuid(), VideoId = v.Id, UserId = user.Id, GroupId = group.Id };

        SeedDbContext.AddRange(user, group, membership, v, share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await groupService.GetUserVideosForGroup(user.Id, group.Id, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
        Assert.Empty(result);
    }

    // G9: All groups query returns videos for all groups the user belongs to
    [Fact]
    public async Task GetUserVideosForAllGroups_ReturnsAcrossGroups_ForMember()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var groupA = new GroupDataBuilder().Build();
        var groupB = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var memA = userB.AssignTo(groupA, joinedAt);
        var memB = userB.AssignTo(groupB, joinedAt);

        var vA = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(1)).Build();
        var vB = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(2)).Build();

        SeedDbContext.AddRange(user, groupA, groupB, memA, memB, vA, vB);
        SeedDbContext.AddRange(
            new SharedWith { Id = Guid.NewGuid(), VideoId = vA.Id, UserId = user.Id, GroupId = groupA.Id },
            new SharedWith { Id = Guid.NewGuid(), VideoId = vB.Id, UserId = user.Id, GroupId = groupB.Id }
        );
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await groupService.GetUserVideosForAllGroups(user.Id, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
        Assert.Equal(2, result.Length);
        // Ordered by recorded desc
        Assert.Equal(new[] { vB.Id, vA.Id }, result.Select(r => r.Video.Id).ToArray());
        Assert.Contains(result, r => r.GroupId == groupA.Id && r.Video.Id == vA.Id);
        Assert.Contains(result, r => r.GroupId == groupB.Id && r.Video.Id == vB.Id);
    }

    // G10: All groups query excludes groups without membership
    [Fact]
    public async Task GetUserVideosForAllGroups_ExcludesGroupsWithoutMembership()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var groupA = new GroupDataBuilder().Build();
        var groupB = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var memA = userB.AssignTo(groupA, joinedAt);

        var vA = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(1)).Build();
        var vB = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(2)).Build();

        SeedDbContext.AddRange(user, groupA, groupB, memA, vA, vB);
        SeedDbContext.AddRange(
            new SharedWith { Id = Guid.NewGuid(), VideoId = vA.Id, UserId = user.Id, GroupId = groupA.Id },
            new SharedWith { Id = Guid.NewGuid(), VideoId = vB.Id, UserId = user.Id, GroupId = groupB.Id }
        );
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await groupService.GetUserVideosForAllGroups(user.Id, TestContext.Current.CancellationToken);
        Assert.Single(result);
        Assert.Equal(vA.Id, result.Single().Video.Id);
        Assert.Equal(groupA.Id, result.Single().GroupId);
    }

    // G11: GetAllVideos resolves a thumbnail URL via IThumbnailUrlService when the video has a thumbnail blob
    [Fact]
    public async Task GetAllVideos_PopulatesThumbnailUrl_WhenThumbnailBlobIdIsSet()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        var v = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(1)).WithThumbnailBlobId("thumb-1").Build();
        var share = new SharedWith { Id = Guid.NewGuid(), VideoId = v.Id, UserId = user.Id, GroupId = group.Id };

        SeedDbContext.AddRange(user, group, membership, v, share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        thumbnailUrlService.GetThumbnailUrl("thumb-1").Returns("https://azurite/thumbnails/thumb-1?sv=stub");

        var result = await groupService.GetAllVideos(user.Id, group.Id, TestContext.Current.CancellationToken);
        Assert.Single(result);
        Assert.Equal("https://azurite/thumbnails/thumb-1?sv=stub", result.Single().ThumbnailUrl);

        var resultAllGroups = await groupService.GetAllVideos(user.Id, TestContext.Current.CancellationToken);
        Assert.Single(resultAllGroups);
        Assert.Equal("https://azurite/thumbnails/thumb-1?sv=stub", resultAllGroups.Single().ThumbnailUrl);
    }

    // G12: GetAllVideos leaves ThumbnailUrl null when the video has no thumbnail blob
    [Fact]
    public async Task GetAllVideos_LeavesThumbnailUrlNull_WhenNoThumbnailBlobId()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        var v = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(1)).Build();
        var share = new SharedWith { Id = Guid.NewGuid(), VideoId = v.Id, UserId = user.Id, GroupId = group.Id };

        SeedDbContext.AddRange(user, group, membership, v, share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        thumbnailUrlService.GetThumbnailUrl(Arg.Any<string?>()).Returns((string?)null);

        var result = await groupService.GetAllVideos(user.Id, group.Id, TestContext.Current.CancellationToken);
        Assert.Single(result);
        Assert.Null(result.Single().ThumbnailUrl);
    }
}