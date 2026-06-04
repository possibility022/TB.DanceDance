using Application.Features.Videos.Models;
using System;

namespace Application.Features.Videos.Endpoints.Videos
{
    public class RefreshUploadUrlRequest
    {
        public Guid VideoId { get; set; }
    }
    
    public class RefreshUploadUrlResponse : UploadUrlResponse
    {
    }
}