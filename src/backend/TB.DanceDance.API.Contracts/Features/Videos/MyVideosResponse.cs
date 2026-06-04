using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Models;

namespace Application.Features.Videos.Endpoints.Videos
{
    public class MyVideosResponse
    {
        public ICollection<VideoInformation> VideoInformation { get; set; } = Array.Empty<VideoInformation>();
    }
}