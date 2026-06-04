using System;

namespace TB.DanceDance.API.Contracts.Features.Comments
{
    public class DeleteCommentRequest
    {
        /// <summary>The comment id (bound from the route).</summary>
        public Guid CommentId { get; set; }

        /// <summary>Anonymous id (bound from the query string); falls back to the request header.</summary>
        public string? AnonymousId { get; set; }
    }
}