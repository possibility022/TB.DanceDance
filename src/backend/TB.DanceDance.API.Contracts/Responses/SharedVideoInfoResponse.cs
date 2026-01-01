using System;

namespace TB.DanceDance.API.Contracts.Responses
{
    public class SharedVideoInfoResponse
    {
        public Guid VideoId { get; set; }
        public string Name { get; set; }
        public TimeSpan? Duration { get; set; }
        public DateTime RecordedDateTime { get; set; }
    }
}
