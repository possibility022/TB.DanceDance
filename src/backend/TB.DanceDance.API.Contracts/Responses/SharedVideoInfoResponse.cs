using System;

namespace TB.DanceDance.API.Contracts.Responses
{
    public class SharedVideoInfoResponse
    {
        public Guid VideoId { get; set; }
        public string Name { get; set; }
        public TimeSpan? Duration { get; set; }
        public DateTime RecordedDateTime { get; set; }

        /// <summary>
        /// Controls who can see comments on this video.
        /// 0 = Public (anyone with link), 1 = AuthenticatedOnly, 2 = OwnerOnly
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
    }
}
