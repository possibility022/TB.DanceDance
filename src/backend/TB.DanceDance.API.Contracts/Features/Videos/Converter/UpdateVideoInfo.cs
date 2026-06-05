using System;
using TB.DanceDance.API.Contracts.Features.Videos.Models;

namespace TB.DanceDance.API.Contracts.Features.Videos.Converter
{
    public class UpdateVideoInfoRequest
    {
        public Guid VideoId { get; set; }

        public DateTime RecordedDateTime { get; set; }

        public TimeSpan Duration { get; set; }

        public byte[]? Metadata { get; set; }
    }

    public class VideoToTransformResponse
    {
        public bool VideoExists { get; set; }
        public VideoToTransformModel? VideoToTransform { get; set; }
    }
}