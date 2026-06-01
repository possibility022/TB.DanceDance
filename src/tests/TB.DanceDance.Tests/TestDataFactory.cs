using TB.DanceDance.Access.Domain.Entities;
using TB.DanceDance.Videos.Domain.Entities;

namespace TB.DanceDance.Tests;

public static class TestDataFactory
{
    /// <summary>
    /// Scenario (a): One User assigned to one Group, and the Group has one video (shared with the group).
    /// Returns all created entities and linking records. User/Group/membership belong to the Access
    /// module; Video/SharedWith belong to the Videos module — seed each into its own DbContext.
    /// </summary>
    public static (User user, Group group, AssignedToGroup membership, Video video, SharedWith groupShare)
        OneUserAssignedToOneGroup_WithOneVideo()
    {
        // Create user and group
        var userB = new UserDataBuilder();
        var user = userB.Build();

        var groupB = new GroupDataBuilder();
        var group = groupB.Build();

        // Assign user to group (join now)
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        // Create one video uploaded by the user and share it with the group (recorded after join)
        var videoB = new VideoDataBuilder()
            .UploadedBy(user)
            .RecordedAt(joinedAt.AddMinutes(1))
            .ShareWithGroup(group, user);
        var video = videoB.Build();
        var groupShare = video.SharedWith.Single();

        return (user, group, membership, video, groupShare);
    }

    /// <summary>
    /// Scenario (b): One User assigned to one Group, the Group has two videos.
    /// The first video was recorded BEFORE the user joined the group; the second AFTER.
    /// Returns the created entities, membership, and both videos with their share links.
    /// </summary>
    public static (
        User user,
        Group group,
        AssignedToGroup membership,
        Video videoBeforeJoin,
        SharedWith shareBeforeJoin,
        Video videoAfterJoin,
        SharedWith shareAfterJoin)
        OneUserAssignedToOneGroup_WithTwoVideos_OneBeforeJoin()
    {
        // Create user and group
        var userB = new UserDataBuilder();
        var user = userB.Build();

        var groupB = new GroupDataBuilder();
        var group = groupB.Build();

        // User joins the group at a specific time
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        // Video recorded BEFORE join
        var videoBefore = new VideoDataBuilder()
            .UploadedBy(user)
            .RecordedAt(joinedAt.AddMinutes(-10))
            .ShareWithGroup(group, user)
            .Build();
        var shareBefore = videoBefore.SharedWith.Single();

        // Video recorded AFTER join
        var videoAfter = new VideoDataBuilder()
            .UploadedBy(user)
            .RecordedAt(joinedAt.AddMinutes(10))
            .ShareWithGroup(group, user)
            .Build();
        var shareAfter = videoAfter.SharedWith.Single();

        return (user, group, membership, videoBefore, shareBefore, videoAfter, shareAfter);
    }

    /// <summary>
    /// Scenario (c): One User assigned to an Event. Event has one video (shared with the event).
    /// Returns all created entities and linking records.
    /// </summary>
    public static (User user, User owner, Event evt, AssignedToEvent participation, Video video, SharedWith eventShare)
        OneUserAssignedToEvent_WithOneVideo()
    {
        // Create user and event
        var userB = new UserDataBuilder();
        var user = userB.Build();

        var eventB = new EventDataBuilder();
        var owner = eventB.BuildOwner();
        var evt = eventB.Build();

        // Assign user to event
        var participation = userB.AssignTo(evt);

        // Create one video uploaded by the user and share it with the event
        var videoB = new VideoDataBuilder()
            .UploadedBy(user)
            .RecordedAt(DateTime.UtcNow.AddMinutes(1))
            .ShareWithEvent(evt, user);
        var video = videoB.Build();
        var eventShare = video.SharedWith.Single();

        return (user, owner, evt, participation, video, eventShare);
    }
}
