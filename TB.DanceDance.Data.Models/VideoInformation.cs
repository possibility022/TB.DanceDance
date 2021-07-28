using System;
using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.Data.Models
{
    public class VideoInformation
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string BlobId { get; set; }

        public DanceType Type { get; set; }

        public DateTime CreationTimeUtc { get; set; }
        public TimeSpan? Duration { get; set; }

        public byte[] MetadataAsJson { get; set; }
    }
}
