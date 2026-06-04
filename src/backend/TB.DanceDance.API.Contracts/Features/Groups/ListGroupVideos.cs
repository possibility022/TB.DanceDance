using Application.Features.Groups.Models;
using System;

namespace Application.Features.Groups.Endpoints
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