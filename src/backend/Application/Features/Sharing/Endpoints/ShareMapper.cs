using Domain.Entities;
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
                VideoId = link.VideoId,
                VideoName = link.Video?.Name ?? string.Empty,
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
                AllowAnonymousCommentsOnThisLink = link.AllowAnonymousComments
            };
        }
    }
}
