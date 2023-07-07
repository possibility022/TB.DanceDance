using System;

namespace TB.DanceDance.API.Contracts
{
    public class UpdateVideoInfoRequest
    {
        public Guid VideoId { get; set; }
        public DateTime RecordedDateTime { get; set; }
        public TimeSpan Duration { get; set; }
        public byte[]? Metadata { get; set; }
    }
}
