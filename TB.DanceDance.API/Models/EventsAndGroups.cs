using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.API.Models
{
    public record EventsAndGroups
    {
        public ICollection<Event> Events { get; set; } = new List<Event>();
        
        public ICollection<Group> Groups { get; set; } = new List<Group>();
    }
}
