using Application.Extensions;
using Domain.Entities;
using FastEndpoints;

namespace Application.Features.Events.Endpoints;

public record CreateNewEventResponse(Guid Id);

public class AddEventEndpoint : Endpoint<CreateNewEventRequest, CreateNewEventResponse>
{
    private readonly IEventService eventService;

    public AddEventEndpoint(IEventService eventService)
    {
        this.eventService = eventService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Events.AddEvent);
    }
    
    public override async Task HandleAsync(CreateNewEventRequest req, CancellationToken ct)
    {
        var userId = User.GetSubject();

        var @event = new Event()
        {
            Date = req.Event.Date.ToUniversalTime(), Name = req.Event.Name, Type = EventType.Unknown, Owner = userId,
        };
        
        var newEventEntity = await eventService.CreateEventAsync(@event, ct);
        await Send.OkAsync(new CreateNewEventResponse(newEventEntity.Id), ct);
    }
}