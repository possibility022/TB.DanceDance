using System;

namespace TB.DanceDance.API.Contracts
{
    public class VideoToTransform
    {
        public Guid Id { get; set; }

        public string Sas { get; set; } = null!;
    }
}
