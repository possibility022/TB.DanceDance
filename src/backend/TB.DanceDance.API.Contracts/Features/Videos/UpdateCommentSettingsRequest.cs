using System;
using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.API.Contracts.Features.Videos
{
    public class UpdateCommentSettingsRequest
    {
        public Guid VideoId { get; set; }
        /// <summary>
        /// Controls who can see comments on this video.
        /// 0 = Public (anyone with link), 1 = AuthenticatedOnly, 2 = OwnerOnly
        /// </summary>
        [Required]
        [Range(0, 2)]
        public int CommentVisibility { get; set; }
    }
}