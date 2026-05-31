using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Contracts;

/// <summary>
/// Returns every event in the system. Used by the API to build the "all events" listing and to
/// compute the events still available for a user to request.
/// </summary>
public record GetAllEventsQuery : IRequest<IReadOnlyCollection<EventDto>>
{
}
