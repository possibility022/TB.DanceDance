using Domain.Entities;
using TB.DanceDance.API.Contracts.Features.Sharing;
using SharedLinkResponse = TB.DanceDance.API.Contracts.Features.Sharing.SharedLinkResponse;
using SharedVideoInfoResponse = TB.DanceDance.API.Contracts.Features.Sharing.SharedVideoInfoResponse;

namespace Application.Features.Sharing.Endpoints
{
    /// <summary>
    /// Shared helpers for the sharing endpoints: builds the public share URL and projects a
    /// <see cref="SharedLink"/> to its API contracts. Ported from the former <c>ShareController</c>.
    /// </summary>
    internal static class ShareMapper
    {
        /// <summary>Builds the public, front-end share URL for a link id.</summary>
        public static string ResolveLinkUrl(string appWebsiteOrigin, string linkId)
            => $"{appWebsiteOrigin}/shared/{linkId}";

        /// <summary>Projects a shared link to its API response, including the resolved share URL.</summary>
        public static SharedLinkResponse MapToSharedLinkResponse(SharedLink link, string appWebsiteOrigin)
        {
            return new SharedLinkResponse
            {
                LinkId = link.Id,
                VideoId = link.VideoId ?? System.Guid.Empty,
                VideoName = link.Video?.Name ?? string.Empty,
                CompetitionId = link.CompetitionId,
                CompetitionName = link.Competition?.Name,
                CreatedAt = link.CreatedAt,
                ExpireAt = link.ExpireAt,
                IsRevoked = link.IsRevoked,
                ShareUrl = ResolveLinkUrl(appWebsiteOrigin, link.Id),
                AllowComments = link.AllowComments,
                AllowAnonymousComments = link.AllowAnonymousComments
            };
        }

        /// <summary>
        /// Projects the video behind a shared link to the anonymous-facing info response, combining
        /// video metadata with the comment settings of this specific link.
        /// </summary>
        public static SharedVideoInfoResponse MapToSharedVideoInfoResponse(SharedLink link)
        {
            var video = link.Video!;

            return new SharedVideoInfoResponse
            {
                VideoId = video.Id,
                Name = video.Name,
                Duration = video.Duration,
                RecordedDateTime = video.RecordedDateTime,
                CommentVisibility = (int)video.CommentVisibility,
                AllowCommentsOnThisLink = link.AllowComments,
                AllowAnonymousCommentsOnThisLink = link.AllowAnonymousComments,
                IsCompetition = false
            };
        }

        /// <summary>
        /// Projects the competition behind a shared link to the anonymous-facing info response: the
        /// competition metadata plus all of its videos, combined with this link's comment settings.
        /// </summary>
        public static SharedVideoInfoResponse MapToSharedCompetitionInfoResponse(SharedLink link)
        {
            var competition = link.Competition!;

            return new SharedVideoInfoResponse
            {
                VideoId = System.Guid.Empty,
                Name = competition.Name,
                Duration = null,
                RecordedDateTime = competition.Date ?? competition.CreatedDateTime,
                CommentVisibility = (int)competition.CommentVisibility,
                AllowCommentsOnThisLink = link.AllowComments,
                AllowAnonymousCommentsOnThisLink = link.AllowAnonymousComments,
                IsCompetition = true,
                Videos = (competition.Videos ?? new System.Collections.Generic.List<Video>())
                    .OrderBy(v => v.RecordedDateTime)
                    .Select(v => new SharedVideoItem
                    {
                        VideoId = v.Id,
                        Name = v.Name,
                        Duration = v.Duration,
                        RecordedDateTime = v.RecordedDateTime
                    })
                    .ToArray()
            };
        }
    }
}
