using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.API.Contracts.Requests
{
    public class CreateSharedLinkRequest
    {
        /// <summary>
        /// Number of days until the link expires. Valid range: 1-365 days. Default is 7 days.
        /// </summary>
        [Range(1, 365)]
        public int ExpirationDays { get; set; } = 7;

        /// <summary>
        /// Whether commenting is allowed through this shared link. Default is true.
        /// </summary>
        public bool AllowComments { get; set; } = true;

        /// <summary>
        /// Whether anonymous (non-logged-in) users can comment through this shared link. Default is false.
        /// Only applies if AllowComments is true.
        /// </summary>
        public bool AllowAnonymousComments { get; set; } = false;
    }
}
