using MongoDB.Bson.Serialization.Attributes;

namespace TB.DanceDance.Data.MongoDb.Models
{
    public class VideoInformation
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; init; }

        public string BlobId { get; init; }
        
        public VideoOwner VideoOwner { get; set; }

        public DateTime CreationTimeUtc { get; init; }
        public TimeSpan? Duration { get; init; }

        public string MetadataAsJson { get; init; }

    }

    
    // Todo, make a record
    public class Event
    {
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public string Id { get; set; }
        public string Name { get; init; }
        public DateTimeOffset Date { get; init; }
        
        /// <summary>
        /// A lit of user subjects.
        /// </summary>
        public ICollection<string> Attenders { get; init; }
    }

    // Todo, make a record
    public class Group
    {
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public string Id { get; set; }
        public string GroupName { get; set; }
        
        /// <summary>
        /// A list of user subjects.
        /// </summary>
        public ICollection<string> People { get; set; }
    }

    // Todo, make a record
    public class VideoOwner
    {
        /// <summary>
        /// Id of a group, event or person.
        /// </summary>
        public string OwnerId { get; init; }
        public OwnerType OwnerType { get; init; }
    }

    public enum OwnerType
    {
        NotSpecified,
        Person,
        Group,
        Event
    } 
}
