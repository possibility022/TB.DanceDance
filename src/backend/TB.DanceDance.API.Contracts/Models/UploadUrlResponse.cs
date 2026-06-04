using System;

namespace TB.DanceDance.API.Contracts.Models
{
    public class UploadUrlResponse
    {
        public string Sas { get; set; }
        public Guid VideoId { get; set; }
        public DateTimeOffset ExpireAt { get; set; }
    }
}