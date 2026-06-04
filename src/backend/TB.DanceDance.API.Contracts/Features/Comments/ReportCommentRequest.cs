using System;

namespace Application.Features.Comments.Endpoints
{
    public class ReportCommentRequest
    {
        /// <summary>The comment id (bound from the route).</summary>
        public Guid CommentId { get; set; }

        /// <summary>The reason for reporting this comment.</summary>
        public string Reason { get; set; } = null!;
    }
}