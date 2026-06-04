using TB.DanceDance.API.Contracts.Models;

namespace Application.Features.Videos.Endpoints.Videos
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