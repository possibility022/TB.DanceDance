namespace TB.DanceDance.Mobile.Data.Models;

public record Event
{
    public string Name { get; set; }
    public DateTime When { get; set; }
    public Guid Id { get; set; }

    public static Event MapFromApiEvent(TB.DanceDance.API.Contracts.Models.Event r)
    {
        return new Event() { Name = r.Name, When = r.Date, Id = r.Id };
    }
}