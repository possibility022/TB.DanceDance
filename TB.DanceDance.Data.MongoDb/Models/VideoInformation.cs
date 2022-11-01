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

        public DateTime CreationTimeUtc { get; init; }
        public TimeSpan? Duration { get; init; }

        public string MetadataAsJson { get; init; }

    }
}
