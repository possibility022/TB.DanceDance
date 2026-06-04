using Application.Features.Groups.Models;
using Domain.Entities;

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
}