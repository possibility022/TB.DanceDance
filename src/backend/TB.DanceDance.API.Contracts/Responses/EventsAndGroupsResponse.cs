using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.API.Contracts.Responses
{

    public class EventsAndGroupsResponse : EventsAndGroups
    {

    }

    public class UserEventsAndGroupsResponse
    {
        public EventsAndGroups Assigned { get; set; } = new EventsAndGroups();
        public EventsAndGroups Available { get; set; } = new EventsAndGroups();
        public EventsAngGroupsIds Pending { get; set; } = new EventsAngGroupsIds(); // waiting for approval
    }

    public class EventsAngGroupsIds
    {
        public IReadOnlyCollection<Guid> Events { get; set; } = Array.Empty<Guid>();

        public IReadOnlyCollection<Guid> Groups { get; set; } = Array.Empty<Guid>();
    }

    public class EventsAndGroups
    {
        public ICollection<Event> Events { get; set; } = new List<Event>();

        public ICollection<Group> Groups { get; set; } = new List<Group>();
    }
}