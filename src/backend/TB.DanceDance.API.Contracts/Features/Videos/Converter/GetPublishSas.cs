using System;

namespace TB.DanceDance.API.Contracts.Features.Videos.Converter
{
    public class GetPublishSasRequest
    {
        public Guid VideoId { get; set; }
    }
    
    public class GetPublishSasResponse
    {
        public string Sas { get; set; } = string.Empty;
    }
}