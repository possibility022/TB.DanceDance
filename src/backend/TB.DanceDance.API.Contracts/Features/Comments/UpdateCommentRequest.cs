using System;

namespace TB.DanceDance.API.Contracts.Features.Comments
{
    public class UpdateCommentRequest
    {
        /// <summary>The comment id (bound from the route).</summary>
        public Guid CommentId { get; set; }

        /// <summary>The new comment content.</summary>
        public string Content { get; set; } = null!;

        /// <summary>Anonymous id, used to authorize edits to anonymously-posted comments.</summary>
        public string? AnonymousId { get; set; }

        /// <summary>Display name used when posting as anonymous.</summary>
        public string? AuthorName { get; set; }
    }
}