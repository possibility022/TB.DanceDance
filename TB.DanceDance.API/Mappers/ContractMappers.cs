using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.Services.Models;

namespace TB.DanceDance.API.Mappers;

public class ContractMappers
{
    public static VideoInformationModel MapToVideoInformation(VideoFromGroupInfo info)
    {
        return MapToVideoInformation(info.Video);
    }

    public static VideoInformationResponse MapToVideoInformation(TB.DanceDance.Data.PostgreSQL.Models.Video video)
    {
        return new VideoInformationResponse()
        {
            BlobId = video.BlobId,
            Duration = video.Duration,
            Id = video.Id,
            Name = video.Name,
            RecordedDateTime = video.RecordedDateTime,
            Converted = video.Converted,
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

    public static Data.PostgreSQL.Models.Event MapFromNewEventRequestToEvent(CreateNewEventRequest request)
    {
        return new Data.PostgreSQL.Models.Event()
        {
            Date = request.Event.Date,
            Name = request.Event.Name,
            Type = MapFromEventType(request.Event.Type)
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

    public static Data.PostgreSQL.Models.EventType MapFromEventType(EventType eventType)
    {
        if (eventType == EventType.SmallWorkshop)
        {
            return Data.PostgreSQL.Models.EventType.SmallWorkshop;
        }
        else if (eventType == EventType.MediumNotPointed)
        {
            return Data.PostgreSQL.Models.EventType.MediumNotPointed;
        }
        else if (eventType == EventType.PointedEvent)
        {
            return Data.PostgreSQL.Models.EventType.PointedEvent;
        }
        else if (eventType == EventType.Unknown)
        {
            return Data.PostgreSQL.Models.EventType.Unknown;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(eventType), "Cannot map eventType value: " + eventType.ToString());
        }
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
