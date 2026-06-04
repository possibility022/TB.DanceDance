using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.API.Contracts.Features.Videos
{
    public class MyVideosResponse
    {
        public ICollection<VideoInformation> VideoInformation { get; set; } = Array.Empty<VideoInformation>();
    }
}