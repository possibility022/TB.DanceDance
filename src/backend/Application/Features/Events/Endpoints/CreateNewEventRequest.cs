using System.ComponentModel.DataAnnotations;

namespace Application.Features.Events.Endpoints;

public record CreateNewEventRequest
{
    [Required]
    public EventModel Event { get; set; }
};

public class EventModel
{
    public Guid Id { get; set; }

    [Required]
    [MinLength(5)]
    public string Name { get; set; } = null!;

    [Required]
    public DateTime Date { get; set; }
}