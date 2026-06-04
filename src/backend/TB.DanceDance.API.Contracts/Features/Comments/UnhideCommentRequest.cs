using System;

namespace Application.Features.Comments.Endpoints
{
    public class UnhideCommentRequest
    {
        /// <summary>The comment id (bound from the route).</summary>
        public Guid CommentId { get; set; }
    }
}