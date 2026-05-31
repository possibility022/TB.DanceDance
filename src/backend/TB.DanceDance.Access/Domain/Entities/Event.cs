namespace TB.DanceDance.Access.Domain.Entities;

public class Event
{
    private Event() { }
    
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required DateTime Date { get; init; }
    public required EventType Type { get; init; }
    public required string Owner { get; init; }
    
    public class Factory
    {
        public static Event Create(string name, DateTime date, EventType type, string ownerId)
        {
            return new Event()
            {
                Date = date,
                Name = name,
                Type = type,
                Owner = ownerId,
            };
        }
    }
}
