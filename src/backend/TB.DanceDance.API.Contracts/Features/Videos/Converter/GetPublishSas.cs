using System;

namespace Application.Features.Videos.Endpoints.Converter
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