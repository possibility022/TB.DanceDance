using System;

namespace TB.DanceDance.API.Contracts
{
    public class VideoInformation
    {
        public Guid Id { get; set; }
        public string BlobId { get; set; }
        public string Name { get; set; }
        public DateTime RecordedDateTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool SharedWithEvent { get; set; }
        public bool SharedWithGroup { get; set; }
    }
}
