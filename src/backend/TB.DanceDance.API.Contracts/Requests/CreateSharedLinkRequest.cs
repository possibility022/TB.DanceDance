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
    }
}
