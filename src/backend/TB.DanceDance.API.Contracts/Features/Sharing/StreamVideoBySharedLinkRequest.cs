namespace Application.Features.Sharing.Endpoints
{
    public class StreamVideoBySharedLinkRequest
    {
        /// <summary>The shared link id (bound from the route).</summary>
        public string LinkId { get; set; } = string.Empty;
    }
}