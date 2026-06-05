namespace TB.DanceDance.API.Contracts.Features.Sharing
{
    public class CreateSharedLinkRequest
    {
        /// <summary>Number of days until the link expires (1-365). Default 7.</summary>
        public int ExpirationDays { get; set; } = 7;

        /// <summary>Whether commenting is allowed through this link. Default true.</summary>
        public bool AllowComments { get; set; } = true;

        /// <summary>Whether anonymous commenting is allowed through this link. Default false.</summary>
        public bool AllowAnonymousComments { get; set; } = false;
    }
}