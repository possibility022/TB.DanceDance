using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Domain.Entities;

namespace TB.DanceDance.Access.Mappers;

internal static class EventMappers
{
    extension(Event @event)
    {
        internal EventDto MapToDto()
        {
            return new EventDto()
            {
                Date = @event.Date,
                Id = @event.Id,
                Name = @event.Name,
                Owner = @event.Owner
            };
        }
    }
}