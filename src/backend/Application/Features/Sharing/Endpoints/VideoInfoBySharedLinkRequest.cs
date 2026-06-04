namespace Application.Features.Sharing.Endpoints
{
    public record VideoInfoBySharedLinkRequest
    {
        /// <summary>The shared link id (bound from the route).</summary>
        public string LinkId { get; set; } = null!;
    }
}