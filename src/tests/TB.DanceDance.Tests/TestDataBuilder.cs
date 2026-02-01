using Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace TB.DanceDance.Tests;

public static class TestDataBuilder
{
    public static string RandomEmail(string? prefix = null) => $"{prefix ?? Random.Shared.Next(10000).ToString()}user@test{Random.Shared.Next(10000)}.com";
    public static string RandomUserId() => Random.Shared.Next(100000, 999999).ToString();
    public static string RandomName(string prefix = "Name") => $"{prefix}-{Guid.NewGuid():N}";
    public static Guid NewGuid() => Guid.NewGuid();
    public static DateTime UtcNow => DateTime.UtcNow;
    public static TimeSpan RandomDuration() => TimeSpan.FromSeconds(Random.Shared.Next(30, 600));
}

public class UserDataBuilder
{
    private string _userId;
    private string _firstName;
    private string _lastName;
    private string _email;

    public UserDataBuilder()
    {
        _userId = TestDataBuilder.RandomUserId();
        _email = TestDataBuilder.RandomEmail(_userId);
        _firstName = "John" + Random.Shared.Next(1000);
        _lastName = "Doe" + Random.Shared.Next(1000);
    }

    public UserDataBuilder WithId(string id)
    {
        _userId = id;
        return this;
    }

    public UserDataBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public UserDataBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public UserDataBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public string UserId => _userId;
    public string Email => _email;

    public User Build() => new User
    {
        Id = _userId,
        FirstName = _firstName,
        LastName = _lastName,
        Email = _email
    };

    public AssignedToEvent AssignTo(Event evt) => new AssignedToEvent
    {
        Id = Guid.NewGuid(),
        EventId = evt.Id,
        UserId = _userId
    };

    public AssignedToGroup AssignTo(Group group, DateTime? whenJoined = null) => new AssignedToGroup
    {
        Id = Guid.NewGuid(),
        GroupId = group.Id,
        UserId = _userId,
        WhenJoined = whenJoined ?? DateTime.UtcNow
    };
}

public class GroupDataBuilder
{
    private Guid _id;
    private string _name;
    private DateOnly _seasonStart;
    private DateOnly _seasonEnd;

    public GroupDataBuilder()
    {
        _id = Guid.NewGuid();
        _name = TestDataBuilder.RandomName("Group");
        _seasonStart = DateOnly.FromDateTime(new DateTime(2022, 9, 1));
        _seasonEnd = DateOnly.FromDateTime(new DateTime(2023, 8, 31));
    }

    public GroupDataBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public GroupDataBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public GroupDataBuilder WithSeasonDates(DateOnly start, DateOnly end)
    {
        _seasonStart = start;
        _seasonEnd = end;
        return this;
    }

    public Group Build() => new Group
    {
        Id = _id,
        Name = _name,
        SeasonStart = _seasonStart,
        SeasonEnd = _seasonEnd,
    };

    public AssignedToGroup AddMember(User user, DateTime? whenJoined = null) => new AssignedToGroup
    {
        Id = Guid.NewGuid(),
        GroupId = _id,
        UserId = user.Id,
        WhenJoined = whenJoined ?? DateTime.UtcNow
    };

    public AssignedToGroup AddMember(UserDataBuilder userBuilder, DateTime? whenJoined = null) => new AssignedToGroup
    {
        Id = Guid.NewGuid(),
        GroupId = _id,
        UserId = userBuilder.UserId,
        WhenJoined = whenJoined ?? DateTime.UtcNow
    };
}

public class EventDataBuilder
{
    private Guid _id;
    private string _name;
    private DateTime _date;
    private EventType _type;
    private string _owner;
    private UserDataBuilder _ownerBuilder;

    public EventDataBuilder()
    {
        _id = Guid.NewGuid();
        _name = TestDataBuilder.RandomName("Event");
        _date = DateTime.UtcNow.Date.AddDays(1);
        _type = EventType.PointedEvent;
        _ownerBuilder = new UserDataBuilder();
        _owner = _ownerBuilder.UserId;
    }

    public EventDataBuilder WithId(Guid id) { _id = id; return this; }
    public EventDataBuilder WithName(string name) { _name = name; return this; }
    public EventDataBuilder OnDate(DateTime date) { _date = date; return this; }
    public EventDataBuilder OfType(EventType type) { _type = type; return this; }

    // Allows overriding owner by raw userId
    public EventDataBuilder WithOwner(string userId)
    {
        _owner = userId;
        _ownerBuilder = new UserDataBuilder().WithId(userId);
        return this;
    }

    // Allows overriding owner by existing User entity
    public EventDataBuilder WithOwner(User user)
    {
        _owner = user.Id;
        // Build a simple builder mirroring the provided user
        _ownerBuilder = new UserDataBuilder()
            .WithId(user.Id)
            .WithFirstName(user.FirstName)
            .WithLastName(user.LastName)
            .WithEmail(user.Email);
        return this;
    }

    // Allows overriding owner by a UserDataBuilder
    public EventDataBuilder WithOwner(UserDataBuilder userBuilder)
    {
        _ownerBuilder = userBuilder;
        _owner = userBuilder.UserId;
        return this;
    }

    public string OwnerUserId => _owner;

    // Expose the created owner user for convenience
    public User BuildOwner() => (_ownerBuilder ?? new UserDataBuilder().WithId(_owner)).Build();

    public Event Build() => new Event
    {
        Id = _id,
        Name = _name,
        Date = _date,
        Type = _type,
        Owner = _owner
    };

    public AssignedToEvent AddParticipant(User user) => new AssignedToEvent
    {
        Id = Guid.NewGuid(),
        EventId = _id,
        UserId = user.Id
    };

    public AssignedToEvent AddParticipant(UserDataBuilder userBuilder) => new AssignedToEvent
    {
        Id = Guid.NewGuid(),
        EventId = _id,
        UserId = userBuilder.UserId
    };
}

public class VideoDataBuilder
{
    private Guid _id;
    private string? _blobId;
    private string _name;
    private string _uploadedBy;
    private DateTime _recorded;
    private DateTime _shared;
    private TimeSpan? _duration;
    private string _fileName;
    private string _sourceBlobId;
    private bool _converted;
    private bool _published;
    private long _sourceBlobSize;
    private long _convertedBlobSize;
    private CommentVisibility _commentVisibility;

    private readonly List<SharedWith> _sharedWith = new();

    public VideoDataBuilder()
    {
        _id = Guid.NewGuid();
        _name = TestDataBuilder.RandomName("Video");
        _uploadedBy = TestDataBuilder.RandomUserId();
        _recorded = DateTime.UtcNow.AddDays(-1);
        _shared = DateTime.UtcNow;
        _duration = TestDataBuilder.RandomDuration();
        _fileName = $"{_name}.mp4";
        _sourceBlobId = $"src-{Guid.NewGuid():N}";
        _converted = false;
        _published = true;
        _sourceBlobSize = 0;
        _convertedBlobSize = 0;
        _commentVisibility = CommentVisibility.Public;
    }

    public VideoDataBuilder WithId(Guid id) { _id = id; return this; }
    public VideoDataBuilder WithBlobId(string? blobId) { _blobId = blobId; return this; }
    public VideoDataBuilder WithName(string name) { _name = name; return this; }
    public VideoDataBuilder UploadedBy(string userId) { _uploadedBy = userId; return this; }
    public VideoDataBuilder UploadedBy(User user) { _uploadedBy = user.Id; return this; }
    public VideoDataBuilder UploadedBy(UserDataBuilder userBuilder) { _uploadedBy = userBuilder.UserId; return this; }
    public VideoDataBuilder RecordedAt(DateTime dt) { _recorded = dt; return this; }
    public VideoDataBuilder SharedAt(DateTime dt) { _shared = dt; return this; }
    public VideoDataBuilder WithDuration(TimeSpan? duration) { _duration = duration; return this; }
    public VideoDataBuilder WithFileName(string file) { _fileName = file; return this; }
    public VideoDataBuilder WithSourceBlobId(string src) { _sourceBlobId = src; return this; }
    public VideoDataBuilder Converted(bool value = true) { _converted = value; return this; }
    public VideoDataBuilder WithSourceBlobSize(long size) { _sourceBlobSize = size; return this; }
    public VideoDataBuilder WithConvertedBlobSize(long size) { _convertedBlobSize = size; return this; }
    public VideoDataBuilder WithCommentVisibility(CommentVisibility visibility) { _commentVisibility = visibility; return this; }

    public Video Build() => new Video
    {
        Id = _id,
        BlobId = _blobId,
        Name = _name,
        UploadedBy = _uploadedBy,
        RecordedDateTime = _recorded,
        SharedDateTime = _shared,
        Duration = _duration,
        FileName = _fileName,
        SourceBlobId = _sourceBlobId,
        Converted = _converted,
        SourceBlobSize = _sourceBlobSize,
        ConvertedBlobSize = _convertedBlobSize,
        CommentVisibility = _commentVisibility
    };

    public VideoDataBuilder ShareWithUser(string userId)
    {
        _sharedWith.Add(new SharedWith { Id = Guid.NewGuid(), VideoId = _id, UserId = userId });
        return this;
    }

    public VideoDataBuilder ShareWithUser(User user) => ShareWithUser(user.Id);
    public VideoDataBuilder ShareWithUser(UserDataBuilder userBuilder) => ShareWithUser(userBuilder.UserId);

    /// <summary>
    /// Creates a private video share entry (both EventId and GroupId are null)
    /// </summary>
    public VideoDataBuilder ShareAsPrivate(string userId)
    {
        _sharedWith.Add(new SharedWith { Id = Guid.NewGuid(), VideoId = _id, UserId = userId, EventId = null, GroupId = null });
        return this;
    }

    public VideoDataBuilder ShareAsPrivate(User user) => ShareAsPrivate(user.Id);
    public VideoDataBuilder ShareAsPrivate(UserDataBuilder userBuilder) => ShareAsPrivate(userBuilder.UserId);

    public VideoDataBuilder ShareWithGroup(Group group, string userId)
    {
        _sharedWith.Add(new SharedWith { Id = Guid.NewGuid(), VideoId = _id, UserId = userId, GroupId = group.Id });
        return this;
    }
    public VideoDataBuilder ShareWithGroup(Group group, User user) => ShareWithGroup(group, user.Id);
    public VideoDataBuilder ShareWithGroup(Group group, UserDataBuilder userBuilder) => ShareWithGroup(group, userBuilder.UserId);

    public VideoDataBuilder ShareWithEvent(Event evt, string userId)
    {
        _sharedWith.Add(new SharedWith { Id = Guid.NewGuid(), VideoId = _id, UserId = userId, EventId = evt.Id });
        return this;
    }
    public VideoDataBuilder ShareWithEvent(Event evt, User user) => ShareWithEvent(evt, user.Id);
    public VideoDataBuilder ShareWithEvent(Event evt, UserDataBuilder userBuilder) => ShareWithEvent(evt, userBuilder.UserId);

    public IReadOnlyList<SharedWith> BuildShares() => _sharedWith.ToList();
}

public class SharedLinkDataBuilder
{
    private string _id;
    private Guid _videoId;
    private string _sharedBy;
    private DateTimeOffset _createdAt;
    private DateTimeOffset _expireAt;
    private bool _isRevoked;
    private bool _allowComments;
    private bool _allowAnonymousComments;

    public SharedLinkDataBuilder()
    {
        _id = TestDataBuilder.RandomName("link").Substring(0, 8); // 8-char placeholder
        _videoId = Guid.NewGuid();
        _sharedBy = TestDataBuilder.RandomUserId();
        _createdAt = DateTimeOffset.UtcNow;
        _expireAt = DateTimeOffset.UtcNow.AddDays(7);
        _isRevoked = false;
        _allowComments = true;
        _allowAnonymousComments = false;
    }

    public SharedLinkDataBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public SharedLinkDataBuilder ForVideo(Guid videoId)
    {
        _videoId = videoId;
        return this;
    }

    public SharedLinkDataBuilder ForVideo(Video video)
    {
        _videoId = video.Id;
        return this;
    }

    public SharedLinkDataBuilder SharedBy(string userId)
    {
        _sharedBy = userId;
        return this;
    }

    public SharedLinkDataBuilder SharedBy(User user)
    {
        _sharedBy = user.Id;
        return this;
    }

    public SharedLinkDataBuilder SharedBy(UserDataBuilder userBuilder)
    {
        _sharedBy = userBuilder.UserId;
        return this;
    }

    public SharedLinkDataBuilder CreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public SharedLinkDataBuilder ExpiresInDays(int days)
    {
        _expireAt = _createdAt.AddDays(days);
        return this;
    }

    public SharedLinkDataBuilder ExpiresAt(DateTimeOffset expireAt)
    {
        _expireAt = expireAt;
        return this;
    }

    public SharedLinkDataBuilder Revoked(bool isRevoked = true)
    {
        _isRevoked = isRevoked;
        return this;
    }

    public SharedLinkDataBuilder AllowComments(bool allowComments = true)
    {
        _allowComments = allowComments;
        return this;
    }

    public SharedLinkDataBuilder AllowAnonymousComments(bool allowAnonymous = true)
    {
        _allowAnonymousComments = allowAnonymous;
        _allowComments = true; // Must allow comments to allow anonymous comments
        return this;
    }

    public string LinkId => _id;

    public SharedLink Build() => new SharedLink
    {
        Id = _id,
        VideoId = _videoId,
        SharedBy = _sharedBy,
        CreatedAt = _createdAt,
        ExpireAt = _expireAt,
        IsRevoked = _isRevoked,
        AllowComments = _allowComments,
        AllowAnonymousComments = _allowAnonymousComments
    };
}

public class CommentDataBuilder
{
    private Guid _id;
    private Guid _videoId;
    private string? _userId;
    private string? _sharedLinkId;
    private string _content;
    private DateTimeOffset _createdAt;
    private DateTimeOffset? _updatedAt;
    private bool _isHidden;
    private bool _isReported;
    private string? _reportedReason;
    private string? _anonymouseId;
    private string? _anonymouseName;

    public CommentDataBuilder()
    {
        _id = Guid.NewGuid();
        _videoId = Guid.NewGuid();
        _userId = null; // Can be null for anonymous
        _sharedLinkId = null;
        _content = TestDataBuilder.RandomName("Comment content");
        _createdAt = DateTimeOffset.UtcNow;
        _updatedAt = null;
        _isHidden = false;
        _isReported = false;
        _reportedReason = null;
    }

    public CommentDataBuilder WithAnonymousId(string anonymouseId)
    {
        _anonymouseId = anonymouseId;
        return this;
    }

    public CommentDataBuilder WithAnonymousName(string anonymouseName)
    {
        _anonymouseName = anonymouseName;
        return this;
    }

    public CommentDataBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public CommentDataBuilder ForVideo(Guid videoId)
    {
        _videoId = videoId;
        return this;
    }

    public CommentDataBuilder ForVideo(Video video)
    {
        _videoId = video.Id;
        return this;
    }

    public CommentDataBuilder ByUser(string userId)
    {
        _userId = userId;
        _sharedLinkId = null; // Authenticated comments don't have linkId
        return this;
    }

    public CommentDataBuilder ByUser(User user)
    {
        _userId = user.Id;
        _sharedLinkId = null;
        return this;
    }

    public CommentDataBuilder ByUser(UserDataBuilder userBuilder)
    {
        _userId = userBuilder.UserId;
        _sharedLinkId = null;
        return this;
    }

    public CommentDataBuilder ByAnonymous(string sharedLinkId)
    {
        _userId = null;
        _sharedLinkId = sharedLinkId;
        return this;
    }

    public CommentDataBuilder ByAnonymous(SharedLink link)
    {
        _userId = null;
        _sharedLinkId = link.Id;
        return this;
    }

    public CommentDataBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }

    public CommentDataBuilder CreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public CommentDataBuilder UpdatedAt(DateTimeOffset? updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public CommentDataBuilder Hidden(bool isHidden = true)
    {
        _isHidden = isHidden;
        return this;
    }

    public CommentDataBuilder Reported(bool isReported = true)
    {
        _isReported = isReported;
        return this;
    }

    public CommentDataBuilder WithReportReason(string reason)
    {
        _reportedReason = reason;
        _isReported = true;
        return this;
    }

    public Comment Build()
    {
        byte[]? anonymouseIdSha = null;
        if (_anonymouseId != null)
            anonymouseIdSha = SHA256.HashData(Encoding.UTF8.GetBytes(_anonymouseId));
        
        return new Comment
        {
            Id = _id,
            VideoId = _videoId,
            UserId = _userId,
            SharedLinkId = _sharedLinkId,
            Content = _content,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            IsHidden = _isHidden,
            IsReported = _isReported,
            AnonymousName = _anonymouseName,
            ShaOfAnonymousId = anonymouseIdSha,
            ReportedReason = _reportedReason
        };
    }
}