using System;
using TB.DanceDance.API.Contracts.Features.Groups.Model;

namespace TB.DanceDance.API.Contracts.Features.Groups
{
    public class ListGroupVideosResponse
    {
        public VideoFromGroupInformation[] Videos { get; set; } = Array.Empty<VideoFromGroupInformation>();
    }
}