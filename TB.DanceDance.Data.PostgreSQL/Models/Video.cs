namespace TB.DanceDance.Data.PostgreSQL.Models;

public class Video
{
    public Guid Id { get; set; }
    public string? BlobId { get; set; }
    public string Name { get; set; }

    // User Id
    public required string UploadedBy { get; init; }
    public required DateTime RecordedDateTime { get; set; }
    public required DateTime SharedDateTime { get; init; }
    public required TimeSpan? Duration { get; set; }
    public required string FileName { get; init; }

    /// <summary>
    /// When value is set, it means that it is being converted by some service and if we passed that date it means somethin went wrong and another service can try convert it again.
    /// </summary>
    public DateTime? LockedTill { get; set; } = null;

    /// <summary>
    /// Original video
    /// </summary>
    public required string SourceBlobId { get; init; }

    public bool Converted { get; set; } = false;

    public ICollection<SharedWith> SharedWith { get; set; } = null!;
}

public class VideoMetadata
{
    public Guid Id { get; set; }
    public Guid VideoId { get; set; }
    public byte[] Metadata { get; set; } = null!;
}

public class GroupAssigmentRequest
{
    public Guid Id { get; set; }
    public required string UserId { get; init; }
    public required Guid GroupId { get; init; }
    public required DateTime WhenJoined { get; set; }
}

public class EventAssigmentRequest
{
    public Guid Id { get; set; }
    public required string UserId { get; init; }
    public required Guid EventId { get; init; }
}

public class SharedWith
{
    public Guid Id { get; set; }
    public required Guid VideoId { get; init; }
    public required string UserId { get; init; }
    public Guid? EventId { get; set; }
    public Guid? GroupId { get; set; }

    public Video Video { get; set; } = null!;
    public Event? Event { get; set; }
    public Group? Group { get; set; }
}

public class AssignedToGroup
{
    public Guid Id { get; set; }

    public required Guid GroupId { get; init; }

    public required string UserId { get; init; }

    public required DateTime WhenJoined { get; set; }

    public Group Group { get; set; } = null!;
}

public class AssignedToEvent
{
    public Guid Id { get; set; }

    public required Guid EventId { get; init; }

    public required string UserId { get; init; }

    public Event Event { get; set; } = null!;
}

public class Group
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public ICollection<SharedWith> HasSharedVideos { get; set; } = null!;
}

public class Event
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required DateTime Date { get; init; }
    public required EventType Type { get; init; }

    public ICollection<SharedWith> HasSharedVideos { get; set; } = null!;
}

public enum EventType
{
    Unknown = 0,
    PointedEvent,
    MediumNotPointed,
    SmallWorkshop
}