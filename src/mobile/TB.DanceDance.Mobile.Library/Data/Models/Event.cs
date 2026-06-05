using TB.DanceDance.API.Contracts.Features.Events.Models;

namespace TB.DanceDance.Mobile.Library.Data.Models;

public record Event
{
    public string Name { get; set; } = string.Empty;
    public DateTime When { get; set; }
    public Guid Id { get; set; }

    public static Event MapFromApiEvent(EventModel r)
    {
        return new Event() { Name = r.Name, When = r.Date, Id = r.Id };
    }
}