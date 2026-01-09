using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.API.Extensions;

namespace TB.DanceDance.API.Mappers;

public class ContractMappers
{
    public static VideoInformationModel MapToVideoInformation(Domain.Entities.VideoFromGroupInfo info)
    {
        return MapToVideoInformation(info.Video);
    }

    public static VideoInformationResponse MapToVideoInformation(Domain.Entities.Video video)
    {
        return new VideoInformationResponse()
        {
            BlobId = video.BlobId,
            Duration = video.Duration,
            Id = video.Id,
            Name = video.Name,
            RecordedDateTime = video.RecordedDateTime,
            Converted = video.Converted,
            CommentVisibility = (int)video.CommentVisibility
        };
    }

    public static TB.DanceDance.API.Contracts.Models.Group MapToGroupContract(Domain.Entities.Group group)
    {
        return new TB.DanceDance.API.Contracts.Models.Group()
        {
            Id = group.Id,
            Name = group.Name,
            SeasonStart = group.SeasonStart.ToDateTime(TimeOnly.MinValue),
            SeasonEnd = group.SeasonEnd.ToDateTime(TimeOnly.MaxValue),
        };
    }

    public static Event MapToEventContract(Domain.Entities.Event @event)
    {
        return new Event()
        {
            Date = @event.Date,
            Id = @event.Id,
            Name = @event.Name,
            //Type = MapEventType(@event.Type),
        };
    }
    public static Domain.Entities.Event MapFromNewEventRequestToEvent(CreateNewEventRequest request, System.Security.Claims.ClaimsPrincipal user)
    {
        return new Domain.Entities.Event()
        {
            Date = request.Event.Date.ToUniversalTime(),
            Name = request.Event.Name,
            Type = Domain.Entities.EventType.Unknown,
            Owner = user.GetSubject(),
            //Type = MapFromEventType(request.Event.Type)
        };
    }

    public static Contracts.Responses.RequestedAccessesResponse MapToAccessRequests(ICollection<Domain.Models.RequestedAccess> accessRequests)
    {
        return new RequestedAccessesResponse()
        {
            AccessRequests = accessRequests.Select(r =>
            {
                return new RequestedAccess()
                {
                    Name = r.Name,
                    IsGroup = r.IsGroup,
                    RequestId = r.RequestId,
                    RequestorFirstName = r.RequestorFirstName,
                    RequestorLastName = r.RequestorLastName,
                    WhenJoined = r.WhenJoined,
                };
            }).ToList(),
        };
    }

}
