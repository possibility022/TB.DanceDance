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
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// True when the requesting user is the uploader of this video, i.e. the only user
        /// allowed to delete it. Drives owner-only actions (e.g. Delete) in the clients.
        /// </summary>
        public bool IsOwner { get; set; }
    }
}