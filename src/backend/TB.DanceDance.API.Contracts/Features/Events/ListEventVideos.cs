using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Models;

namespace Application.Features.Events.Endpoints
{
    public class ListEventVideosRequest
    {
        public Guid EventId { get; set; }
    }
    
    public class ListEventVideosResponse
    {
        public IReadOnlyCollection<VideoInformation> Videos { get; set; } = null!;
    }
}