using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Features.Events.Models;
using TB.DanceDance.API.Contracts.Features.Groups.Model;

namespace TB.DanceDance.API.Contracts.Features.AccessManagement.Models
{
    public class GetUserAccessSet
    {
        public ICollection<EventModel> Events { get; set; } = Array.Empty<EventModel>();
        public ICollection<GroupModel> Groups { get; set; } = Array.Empty<GroupModel>();
    }
}