using Domain.Entities;
using Domain.Models;
using TB.DanceDance.API.Contracts.Features.AccessManagement.Models;
using TB.DanceDance.API.Contracts.Features.Events.Models;
using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Models;
using Event = Domain.Entities.Event;
using Group = Domain.Entities.Group;

namespace Application;

public static class ContractMappers
{
    public static VideoInformation MapToVideoInformation(Video video, string? thumbnailUrl, string? currentUserId = null)
    {
        return new VideoInformation()
        {
            BlobId = video.BlobId ?? string.Empty,
            Duration = video.Duration,
            VideoId = video.Id,
            Name = video.Name,
            RecordedDateTime = video.RecordedDateTime,
            Converted = video.Converted,
            CommentVisibility = (int)video.CommentVisibility,
            ThumbnailUrl = thumbnailUrl,
            SizeBytes = video.ConvertedBlobSize,
            IsOwner = currentUserId != null && video.UploadedBy == currentUserId
        };
    }

    public static EventModel MapToEventContract(Event @event)
    {
        return new EventModel()
        {
            Id = @event.Id,
            Name = @event.Name,
            Date = @event.Date,
        };
    }

    public static GroupModel MapToGroupContract(Group group)
    {
        return new GroupModel()
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