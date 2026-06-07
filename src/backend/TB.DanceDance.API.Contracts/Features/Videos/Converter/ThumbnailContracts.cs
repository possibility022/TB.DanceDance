using System;

namespace TB.DanceDance.API.Contracts.Features.Videos.Converter
{
    public class VideoToThumbnailModel
    {
        public Guid Id { get; set; }
        public string BlobId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Sas { get; set; } = string.Empty;
    }

    public class VideoToThumbnailResponse
    {
        public bool VideoExists { get; set; }
        public VideoToThumbnailModel? VideoToThumbnail { get; set; }
    }

    public class GetThumbnailSasResponse
    {
        public string Sas { get; set; } = string.Empty;
    }
}
