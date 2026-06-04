using System;

namespace TB.DanceDance.API.Contracts.Features.Comments
{
    public class HideCommentRequest
    {
        /// <summary>The comment id (bound from the route).</summary>
        public Guid CommentId { get; set; }
    }
}