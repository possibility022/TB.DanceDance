using TB.DanceDance.API.Contracts;
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
}
