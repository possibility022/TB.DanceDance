using System.Collections.Generic;

namespace TB.DanceDance.API.Contracts
{

    public class EventsAndGroups
    {
        public ICollection<Event> Events { get; set; } = new List<Event>();

        public ICollection<Group> Groups { get; set; } = new List<Group>();
    }
}