using Application.Extensions;
using Domain.Entities;
using FastEndpoints;
using FluentValidation;
using TB.DanceDance.API.Contracts.Features.Events;

namespace Application.Features.Events.Endpoints;

public record CreateNewEventResponse(Guid Id);

public class CreateNewEventValidator : Validator<CreateNewEventRequest>
{
    public CreateNewEventValidator()
    {
        RuleFor(x => x.Event)
            .NotNull().WithMessage("Event is required.");

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        When(x => x.Event != null, () =>
        {
            RuleFor(x => x.Event.Name)
                .NotEmpty().WithMessage("Event name is required.")
                .MinimumLength(5).WithMessage("Event name must be at least 5 characters long.");

            RuleFor(x => x.Event.Date)
                .NotEmpty().WithMessage("Event date is required.");
        });
    }
}

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
        Policies(ApiScopes.Read);
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