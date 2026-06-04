using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.API.Contracts.Features.AccessManagement.Models
{
    public class GetUserAccessSet
    {
        public ICollection<Event> Events { get; set; } = Array.Empty<Event>();
        public ICollection<Group> Groups { get; set; } = Array.Empty<Group>();
    }
}