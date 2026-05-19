using System;

namespace TB.DanceDance.API.Contracts.Features.Videos
{
    public class UploadVideoInformationResponse
    {
        public string Sas { get; set; }
        public Guid VideoId { get; set; }
        public DateTimeOffset ExpireAt { get; set; }
    }
}