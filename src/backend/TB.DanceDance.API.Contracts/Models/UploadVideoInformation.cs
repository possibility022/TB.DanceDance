using System;

namespace TB.DanceDance.API.Contracts.Models
{
    public class UploadVideoInformation
    {
        public string Sas { get; set; }
        public Guid VideoId { get; set; }
        public DateTime ExpireAt { get; set; }
    }
}