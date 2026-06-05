using System;

namespace TB.DanceDance.API.Contracts.Features.Videos.Converter
{
    public class  UpdateVideoInfoRequest
    {
        public Guid VideoId { get; set; }

        public DateTime RecordedDateTime { get; set; }

        public TimeSpan Duration { get; set; }

        public byte[]? Metadata { get; set; }
    }
    
    public class VideoToTransformResponse
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;

        public string Sas { get; set; } = string.Empty;
    }
}