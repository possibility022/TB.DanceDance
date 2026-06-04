using System;

namespace Application.Features.Sharing.Endpoints
{
    public class CreateSharedLinkRequest
    {
        /// <summary>The video to share (bound from the route).</summary>
        public Guid VideoId { get; set; }

        /// <summary>Number of days until the link expires (1-365). Default 7.</summary>
        public int ExpirationDays { get; set; } = 7;

        /// <summary>Whether commenting is allowed through this link. Default true.</summary>
        public bool AllowComments { get; set; } = true;

        /// <summary>Whether anonymous commenting is allowed through this link. Default false.</summary>
        public bool AllowAnonymousComments { get; set; } = false;
    }
}