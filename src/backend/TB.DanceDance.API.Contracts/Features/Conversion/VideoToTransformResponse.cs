using System;

namespace TB.DanceDance.API.Contracts.Features.Conversion
{
    public class VideoToTransformResponse
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = null!;

        public string Sas { get; set; } = null!;
    }
}
