using System;

namespace Application.Features.Videos.Endpoints.Converter
{
    public class PublicConvertedVideoRequest
    {
        public Guid VideoId { get; set; }
    }
    
    public class PublicConvertedVideoResponse
    {
        public Guid VideoId { get; set; }
    }
}