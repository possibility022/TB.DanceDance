using Domain.Entities;
using Domain.Models;
using TB.DanceDance.API.Contracts.Models;
using Event = Domain.Entities.Event;
using Group = Domain.Entities.Group;

namespace Application;

public static class ContractMappers
{
    public static VideoInformation MapToVideoInformation(Video video)
    {
        return new VideoInformation()
        {
            BlobId = video.BlobId ?? string.Empty,
            Duration = video.Duration,
            VideoId = video.Id,
            Name = video.Name,
            RecordedDateTime = video.RecordedDateTime,
            Converted = video.Converted,
            CommentVisibility = (int)video.CommentVisibility
        };
    }

    public static TB.DanceDance.API.Contracts.Models.Event MapToEventContract(Event @event)
    {
        return new TB.DanceDance.API.Contracts.Models.Event()
        {
            Id = @event.Id,
            Name = @event.Name,
            Date = @event.Date,
        };
    }

    public static TB.DanceDance.API.Contracts.Models.Group MapToGroupContract(Group group)
    {
        return new TB.DanceDance.API.Contracts.Models.Group()
        {
            Id = group.Id,
            Name = group.Name,
            SeasonStart = group.SeasonStart.ToDateTime(TimeOnly.MinValue),
            SeasonEnd = group.SeasonEnd.ToDateTime(TimeOnly.MaxValue),
        };
    }

    public static RequestedAccessModel MapToRequestedAccess(RequestedAccess r)
    {
        return new RequestedAccessModel()
        {
            Name = r.Name,
            IsGroup = r.IsGroup,
            RequestId = r.RequestId,
            RequestorFirstName = r.RequestorFirstName,
            RequestorLastName = r.RequestorLastName,
            WhenJoined = r.WhenJoined,
        };
    }
}