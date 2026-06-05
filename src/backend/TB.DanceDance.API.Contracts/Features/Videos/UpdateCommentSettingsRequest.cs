namespace TB.DanceDance.API.Contracts.Features.Videos
{
    public class UpdateCommentSettingsRequest
    {
        /// <summary>
        /// Controls who can see comments on this video.
        /// 0 = Public (anyone with link), 1 = AuthenticatedOnly, 2 = OwnerOnly
        /// </summary>
        public int CommentVisibility { get; set; }
    }
}
