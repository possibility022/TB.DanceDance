using System;
using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.API.Contracts.Requests
{
    public class UpdateVideoInfoRequest
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
}
