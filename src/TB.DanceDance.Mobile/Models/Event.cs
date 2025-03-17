using TB.DanceDance.API.Contracts.Responses;

namespace TB.DanceDance.Mobile.Models;

public record Event
{
    public string Name { get; set; }
    public DateTime When { get; set; }
    public Guid Id { get; set; }

    public static List<Event> MapFromApiEvent(UserEventsAndGroupsResponse response)
    {
        return response.Assigned.Events
            .Select(r => new Event() { Name = r.Name, When = r.Date, Id = r.Id })
            .ToList();
    }
}