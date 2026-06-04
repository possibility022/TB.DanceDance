namespace TB.DanceDance.API.Contracts.Features.Sharing
{
    public class RevokeSharedLinkRequest
    {
        /// <summary>The shared link id (bound from the route).</summary>
        public string LinkId { get; set; } = string.Empty;
    }
}