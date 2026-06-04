using System;

namespace TB.DanceDance.API.Contracts.Models
{
    public class VideoInformation
    {
        public Guid VideoId { get; set; }
        public string BlobId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime RecordedDateTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool Converted { get; set; }
        public int CommentVisibility { get; set; }
    }
}