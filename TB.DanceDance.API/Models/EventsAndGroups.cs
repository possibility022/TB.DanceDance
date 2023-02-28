using TB.DanceDance.Data.MongoDb.Models;

namespace TB.DanceDance.API.Models
{
    public record EventsAndGroups
    {
        public ICollection<EventSharingSharingScope> Events { get; set; } = new List<EventSharingSharingScope>();
        
        public ICollection<SharingScopeModel> Groups { get; set; } = new List<SharingScopeModel>();
    }
}
