using System;

namespace TB.DanceDance.API.Contracts.Features.Videos.Models
{
    public class VideoToTransformModel
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;

        public string Sas { get; set; } = string.Empty;
    }
}