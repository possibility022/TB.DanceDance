using TB.DanceDance.Tests.TestsFixture;
using TB.DanceDance.Videos.Contracts;

namespace TB.DanceDance.Tests.Features.Groups;

/// <summary>
/// Group video listing (<see cref="ViewVideosFromGroupQuery"/> / <see cref="ViewVideosFromAllGroupsQuery"/>,
/// Videos module). Membership and join date come from Access via the mediator; only videos recorded
/// after the user joined the group are returned. The queries now return bare <see cref="VideoDto"/>
/// rows (no group id/name), so assertions are on the returned video ids.
/// </summary>
public class GroupServiceTests : BaseTestClass
{
    public GroupServiceTests(DanceDbFixture dbContextFixture) : base(dbContextFixture)
    {
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

        var v = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(1)).ShareWithGroup(group, user).Build();

        SeedAccessContext.AddRange(user, group, membership);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(v);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewVideosFromGroupQuery(user.Id, group.Id), TestContext.Current.CancellationToken);
        Assert.Single(result);
        Assert.Equal(v.Id, result.Single().Id);
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

        var v = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(-5)).ShareWithGroup(group, user).Build();

        SeedAccessContext.AddRange(user, group, membership);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(v);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewVideosFromGroupQuery(user.Id, group.Id), TestContext.Current.CancellationToken);
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

        var vB = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(1)).ShareWithGroup(groupB, user).Build();

        SeedAccessContext.AddRange(user, groupA, groupB, membershipA);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(vB);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewVideosFromGroupQuery(user.Id, groupA.Id), TestContext.Current.CancellationToken);
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

        var v = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(1)).ShareWithUser(user).Build();

        SeedAccessContext.AddRange(user, group, membership);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(v);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewVideosFromGroupQuery(user.Id, group.Id), TestContext.Current.CancellationToken);
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

        var v1 = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddHours(1)).ShareWithGroup(group, user).Build();
        var v2 = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddHours(3)).ShareWithGroup(group, user).Build();
        var v3 = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddHours(2)).ShareWithGroup(group, user).Build();

        SeedAccessContext.AddRange(user, group, membership);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(v1, v2, v3);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewVideosFromGroupQuery(user.Id, group.Id), TestContext.Current.CancellationToken);
        Assert.Equal(new[] { v2.Id, v3.Id, v1.Id }, result.Select(r => r.Id).ToArray());
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
        var v = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(1))
            .ShareWithGroup(group, user).ShareWithGroup(group, user).Build();

        SeedAccessContext.AddRange(user, group, membership);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(v);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewVideosFromGroupQuery(user.Id, group.Id), TestContext.Current.CancellationToken);
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(v.Id, r.Id));
    }

    // G7: No membership -> no videos
    [Fact]
    public async Task GetUserVideosForGroup_ReturnsEmpty_WhenUserNotMember()
    {
        var user = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        var v = new VideoDataBuilder().UploadedBy(user).RecordedAt(DateTime.UtcNow.AddMinutes(1)).ShareWithGroup(group, user).Build();

        SeedAccessContext.AddRange(user, group);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(v);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewVideosFromGroupQuery(user.Id, group.Id), TestContext.Current.CancellationToken);
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

        var v = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt).ShareWithGroup(group, user).Build();

        SeedAccessContext.AddRange(user, group, membership);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(v);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewVideosFromGroupQuery(user.Id, group.Id), TestContext.Current.CancellationToken);
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

        var vA = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(1)).ShareWithGroup(groupA, user).Build();
        var vB = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(2)).ShareWithGroup(groupB, user).Build();

        SeedAccessContext.AddRange(user, groupA, groupB, memA, memB);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(vA, vB);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewVideosFromAllGroupsQuery(user.Id), TestContext.Current.CancellationToken);
        Assert.Equal(2, result.Count);
        // Ordered by recorded desc
        Assert.Equal(new[] { vB.Id, vA.Id }, result.Select(r => r.Id).ToArray());
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

        var vA = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(1)).ShareWithGroup(groupA, user).Build();
        var vB = new VideoDataBuilder().UploadedBy(user).RecordedAt(joinedAt.AddMinutes(2)).ShareWithGroup(groupB, user).Build();

        SeedAccessContext.AddRange(user, groupA, groupB, memA);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(vA, vB);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewVideosFromAllGroupsQuery(user.Id), TestContext.Current.CancellationToken);
        Assert.Single(result);
        Assert.Equal(vA.Id, result.Single().Id);
    }
}
