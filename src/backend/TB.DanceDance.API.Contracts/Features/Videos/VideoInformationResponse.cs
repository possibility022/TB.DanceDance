using System;

namespace TB.DanceDance.API.Contracts.Features.Videos
{
    public class VideoInformationResponse : VideoInformationModel
    {

    }

    public class VideoInformationModel
    {
        public Guid Id { get; set; }
        public string BlobId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime RecordedDateTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool Converted { get; set; }
        public int CommentVisibility { get; set; }
    }
}
