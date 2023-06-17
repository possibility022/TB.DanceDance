namespace TB.DanceDance.Data.PostgreSQL.Models;

public class Video
{
    public Guid Id { get; set; }
    public required string BlobId { get; init; }
    public string Name { get; set; }

    // User Id
    public required string UploadedBy { get; init; }
    public required DateTime RecordedDateTime { get; init; }
    public required DateTime SharedDateTime { get; init; }
    public required TimeSpan? Duration { get; init; }

    // Navigation properties
    public ICollection<SharedWith> SharedWith { get; set; } = null!;
}

public class VideoToTranform
{
    public Guid Id { get; set; }
    public required string BlobId { get; init; }
    public string Name { get; set; }

    // User Id
    public required string UploadedBy { get; init; }
    public required DateTime RecordedDateTime { get; init; }
    public required DateTime SharedDateTime { get; init; }
    public required TimeSpan? Duration { get; init; }

    /// <summary>
    /// Pointing to event or group. Based on AssignedToEvent flag
    /// </summary>
    public required Guid SharedWithId { get; set; }

    /// <summary>
    /// If true, SharedWithId is pointing to event, if false, SharedWithId is pointing to group
    /// </summary>
    public required bool AssignedToEvent { get; set; }
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