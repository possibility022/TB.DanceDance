using System;
using TB.DanceDance.API.Contracts.Features.Videos.Models;
using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.API.Contracts.Features.Videos
{
    public class RefreshUploadUrlRequest
    {
        public Guid VideoId { get; set; }
    }
    
    public class RefreshUploadUrlResponse : UploadUrlResponse
    {
    }
}