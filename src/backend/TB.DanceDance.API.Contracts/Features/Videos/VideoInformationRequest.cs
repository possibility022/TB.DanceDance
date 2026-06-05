using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.API.Contracts.Features.Videos
{
    public class VideoInformationRequest
    {
        public string BlobId { get; set; } = string.Empty;
    }
    
    public class VideoInformationResponse
    {
        public VideoInformation VideoInformation { get; set; } = null!;
    }
}