using MongoDB.Bson.Serialization.Attributes;

namespace TB.DanceDance.Data.MongoDb.Models
{
    // Todo, make a record
    public class VideoInformation
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; init; }

        public string BlobId { get; init; }

        public SharingScope UploadedBy { get; set; }

        public SharingScope SharedWith { get; set; }

        public DateTime RecordedTimeUtc { get; init; }
        public DateTime SharedDateTimeUtc { get; set; }

        public TimeSpan? Duration { get; init; }

        public string MetadataAsJson { get; init; }

    }

    public record RequestedAssigment
    {
        public string UserId { get; init; }

        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public string EntityId { get; init; } = null!;

        public AssignmentType AssignmentType { get; init; }
    }

    public record Event
    {
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public string Id { get; set; }
        public string Name { get; init; }
        public DateTimeOffset Date { get; init; }
        public EventType EventType { get; init; }

        /// <summary>
        /// A lit of user subjects.
        /// </summary>
        public ICollection<string> Attenders { get; init; } = null!;
    }

    // Todo, make a record
    public class Group
    {
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public string Id { get; set; } = null!;
        public string GroupName { get; set; } = null!;

        /// <summary>
        /// A list of user subjects.
        /// </summary>
        public ICollection<string> People { get; set; } = null!;
    }

    public record SharingScope
    {
        /// <summary>
        /// Id of a group, event or person.
        /// </summary>
        public string EntityId { get; init; }
        public AssignmentType Assignment { get; init; }
    }

    public record SharedVideo()
    {
        public VideoInformation VideoInformation { get; init; }
        public DateTimeOffset Shared { get; init; }
    }

    public enum EventType
    {
        Unknown = 0,
        PointedEvent,
        MediumNotPointed,
        SmallWorkshop
    }

    public enum AssignmentType
    {
        NotSpecified,
        Person,
        Group,
        Event
    }
}
