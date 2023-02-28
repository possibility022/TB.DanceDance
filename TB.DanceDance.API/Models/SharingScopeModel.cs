using TB.DanceDance.Data.MongoDb.Models;

namespace TB.DanceDance.API.Models
{
    public record SharingScopeModel
    {
        public string Name { get; init; }
        public string Id { get; init; }
        public AssignmentType Assignment { get; init; }
    }

    public record EventSharingSharingScope : SharingScopeModel
    {
        public EventType Type { get; init; }
    }
}
