using System;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Videos.Endpoints.Converter
{
    public class  UpdateVideoInfoRequest
    {
        [Required]
        public Guid VideoId { get; set; }

        [Required]
        public DateTime RecordedDateTime { get; set; }

        [Required]
        public TimeSpan Duration { get; set; }

        [Required]
        public byte[]? Metadata { get; set; }
    }
    
    public class VideoToTransformResponse
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;

        public string Sas { get; set; } = string.Empty;
    }
}