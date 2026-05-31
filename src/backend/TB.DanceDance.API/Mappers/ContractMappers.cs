using TB.DanceDance.Access.Contracts;
using TB.DanceDance.API.Contracts.Features.AccessManagement;
using TB.DanceDance.API.Contracts.Features.Videos;
using TB.DanceDance.Videos.Contracts;
using ApiEvent = TB.DanceDance.API.Contracts.Models.Event;
using ApiGroup = TB.DanceDance.API.Contracts.Models.Group;
using ApiRequestedAccess = TB.DanceDance.API.Contracts.Models.RequestedAccess;

namespace TB.DanceDance.API.Mappers;

public class ContractMappers
{
    public static VideoInformationResponse MapToVideoInformation(VideoDto video)
    {
        return new VideoInformationResponse()
        {
            BlobId = video.BlobId,
            Duration = video.Duration,
            Id = video.Id,
            Name = video.Name,
            RecordedDateTime = video.RecordedDateTime,
            Converted = video.Converted,
            CommentVisibility = video.CommentVisibility
        };
    }

    public static ApiGroup MapToGroupContract(GroupDto group)
    {
        return new ApiGroup()
        {
            Id = group.Id,
            Name = group.Name,
            SeasonStart = group.SeasonStart.ToDateTime(TimeOnly.MinValue),
            SeasonEnd = group.SeasonEnd.ToDateTime(TimeOnly.MaxValue),
        };
    }

    public static ApiEvent MapToEventContract(EventDto @event)
    {
        return new ApiEvent()
        {
            Date = @event.Date,
            Id = @event.Id,
            Name = @event.Name,
        };
    }

    public static RequestedAccessesResponse MapToAccessRequests(IReadOnlyCollection<RequestedAccess> accessRequests)
    {
        return new RequestedAccessesResponse()
        {
            AccessRequests = accessRequests.Select(r => new ApiRequestedAccess()
            {
                Name = r.Name,
                IsGroup = r.IsGroup,
                RequestId = r.RequestId,
                RequestorFirstName = r.RequestorFirstName,
                RequestorLastName = r.RequestorLastName,
                WhenJoined = r.WhenJoined,
            }).ToList(),
        };
    }
}
