using System;

namespace Application.Features.Comments.Endpoints
{
    public class HideCommentRequest
    {
        /// <summary>The comment id (bound from the route).</summary>
        public Guid CommentId { get; set; }
    }
}