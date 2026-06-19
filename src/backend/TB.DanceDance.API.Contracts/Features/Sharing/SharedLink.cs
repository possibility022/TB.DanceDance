using System;
using System.Collections.Generic;

namespace TB.DanceDance.API.Contracts.Features.Sharing
{
    public class SharedLinkResponse
    {
        public string LinkId { get; set; }
        /// <summary>The targeted video, or <see cref="Guid.Empty"/> when the link targets a competition.</summary>
        public Guid VideoId { get; set; }
        public string VideoName { get; set; }
        /// <summary>The targeted competition, when the link targets a competition; null otherwise.</summary>
        public Guid? CompetitionId { get; set; }
        public string? CompetitionName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpireAt { get; set; }
        public bool IsRevoked { get; set; }
        public string ShareUrl { get; set; }
        public bool AllowComments { get; set; }
        public bool AllowAnonymousComments { get; set; }
    }

    /// <summary>One video inside a shared competition, with the metadata the viewer needs to play it.</summary>
    public class SharedVideoItem
    {
        public Guid VideoId { get; set; }
        public string Name { get; set; }
        public TimeSpan? Duration { get; set; }
        public DateTime RecordedDateTime { get; set; }
    }

    public class SharedVideoInfoResponse
    {
        /// <summary>The video id for a single-video link; <see cref="Guid.Empty"/> for a competition link.</summary>
        public Guid VideoId { get; set; }
        /// <summary>The video name for a single-video link; the competition name for a competition link.</summary>
        public string Name { get; set; }
        public TimeSpan? Duration { get; set; }
        public DateTime RecordedDateTime { get; set; }

        /// <summary>
        /// Controls who can see comments on this video/competition.
        /// 0 = AuthenticatedOnly, 1 = OwnerOnly, 2 = Public.
        /// </summary>
        public int CommentVisibility { get; set; }

        /// <summary>
        /// Whether commenting is allowed through this specific shared link.
        /// </summary>
        public bool AllowCommentsOnThisLink { get; set; }

        /// <summary>
        /// Whether anonymous commenting is allowed through this specific shared link.
        /// </summary>
        public bool AllowAnonymousCommentsOnThisLink { get; set; }

        /// <summary>True when this link targets a competition (multiple videos, one combined thread).</summary>
        public bool IsCompetition { get; set; }

        /// <summary>The competition's videos when <see cref="IsCompetition"/> is true; empty otherwise.</summary>
        public IReadOnlyCollection<SharedVideoItem> Videos { get; set; } = Array.Empty<SharedVideoItem>();
    }
}
