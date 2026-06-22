using Application.Features.Videos;
using Domain.Entities;
using TB.DanceDance.API.Contracts.Features.Competitions;

namespace Application.Features.Competitions.Endpoints;

public static class CompetitionMapper
{
    public static CompetitionSummaryResponse MapToSummary(Competition competition) => new()
    {
        Id = competition.Id,
        Name = competition.Name,
        Date = competition.Date,
        Location = competition.Location,
        CommentVisibility = (int)competition.CommentVisibility,
        CreatedDateTime = competition.CreatedDateTime,
        VideoCount = competition.Videos?.Count ?? 0
    };

    public static CompetitionResponse MapToResponse(
        Competition competition,
        IThumbnailUrlService thumbnailUrlService,
        string currentUserId) => new()
    {
        Id = competition.Id,
        Name = competition.Name,
        Date = competition.Date,
        Location = competition.Location,
        CommentVisibility = (int)competition.CommentVisibility,
        CreatedDateTime = competition.CreatedDateTime,
        Videos = (competition.Videos ?? [])
            .OrderBy(v => v.RecordedDateTime)
            .Select(v => ContractMappers.MapToVideoInformation(
                v, thumbnailUrlService.GetThumbnailUrl(v.ThumbnailBlobId), currentUserId))
            .ToArray()
    };
}
