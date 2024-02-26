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
    }

    public class EventsAndGroups
    {
        public ICollection<Event> Events { get; set; } = new List<Event>();

        public ICollection<Group> Groups { get; set; } = new List<Group>();
    }
}