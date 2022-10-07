using System;
using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.Data.Models
{
    public class VideoInformation
    {
        [Key]
        public int Id { get; init; }

        public string Name { get; init; }

        public string BlobId { get; init; }

        public DateTime CreationTimeUtc { get; init; }
        public TimeSpan? Duration { get; init; }

        public string MetadataAsJson { get; init; }

        public string PartitionKey { get => "VideoInformation"; set { if (value != "VideoInformation") throw new Exception("Wrong partition key."); } }

    }
}
