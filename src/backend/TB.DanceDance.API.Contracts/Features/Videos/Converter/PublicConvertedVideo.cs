using System;

namespace TB.DanceDance.API.Contracts.Features.Videos.Converter
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