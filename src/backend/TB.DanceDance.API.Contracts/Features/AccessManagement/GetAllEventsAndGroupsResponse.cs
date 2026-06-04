using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Models;

namespace Application.Features.AccessManagement.Endpoints
{
    public class GetAllEventsAndGroupsResponse
    {
        public ICollection<Event> Events { get; set; } = Array.Empty<Event>();
        public ICollection<Group> Groups { get; set; } = Array.Empty<Group>();
    }
}