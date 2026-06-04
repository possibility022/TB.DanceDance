using System;
using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.API.Contracts.Features.Groups
{
    public class ListGroupVideosRequest
    {
        public Guid GroupId { get; set; }
    }
    
    public class ListGroupVideosResponse
    {
        public VideoFromGroupInformation[] Videos { get; set; } = Array.Empty<VideoFromGroupInformation>();
    }
}