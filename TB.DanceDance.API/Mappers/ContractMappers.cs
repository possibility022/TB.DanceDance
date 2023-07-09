using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.Services.Models;

namespace TB.DanceDance.API.Mappers;

public class ContractMappers
{
    public static VideoInformation MapToVideoInformation(VideoInfo info)
    {
        return new VideoInformation()
        {
            BlobId = info.Video.BlobId,
            Duration = info.Video.Duration,
            Id = info.Video.Id,
            Name = info.Video.Name,
            RecordedDateTime = info.Video.RecordedDateTime,
            SharedWithEvent = info.SharedWithEvent,
            SharedWithGroup = info.SharedWithGroup,
        };
    }

    public static Group MapToGroupContract(Data.PostgreSQL.Models.Group group)
    {
        return new Group()
        {
            Id = group.Id,
            Name = group.Name,
        };
    }

    public static Event MapToEventContract(TB.DanceDance.Data.PostgreSQL.Models.Event @event)
    {
        return new Event()
        {
            Date = @event.Date,
            Id = @event.Id,
            Name = @event.Name,
            Type = MapEventType(@event.Type),
        };
    }

    public static EventType MapEventType(Data.PostgreSQL.Models.EventType eventType)
    {
        if (eventType == Data.PostgreSQL.Models.EventType.SmallWorkshop)
        {
            return EventType.SmallWorkshop;
        }
        else if (eventType == Data.PostgreSQL.Models.EventType.MediumNotPointed)
        {
            return EventType.MediumNotPointed;
        }
        else if (eventType == Data.PostgreSQL.Models.EventType.PointedEvent)
        {
            return EventType.PointedEvent;
        }
        else if (eventType == Data.PostgreSQL.Models.EventType.Unknown)
        {
            return EventType.Unknown;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(eventType), "Cannot map eventType value: " + eventType.ToString());
        }
    }
}
