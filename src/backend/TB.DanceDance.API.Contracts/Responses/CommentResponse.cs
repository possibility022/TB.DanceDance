using System;

namespace TB.DanceDance.API.Contracts.Responses
{
    public class CommentResponse
    {
        /// <summary>
        /// The comment ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The video ID this comment belongs to.
        /// </summary>
        public Guid VideoId { get; set; }

        /// <summary>
        /// The name of the user who created the comment.
        /// </summary>
        public string? AuthorName { get; set; }
        
        /// <summary>
        /// Indicates if comment was added as anonymouse.
        /// </summary>
        public bool PostedAsAnonymous { get; set; }

        /// <summary>
        /// The comment content.
        /// </summary>
        public string Content { get; set; } = null!;

        /// <summary>
        /// When the comment was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// When the comment was last updated. Null if never updated.
        /// </summary>
        public DateTimeOffset? UpdatedAt { get; set; }

        /// <summary>
        /// Whether the comment is hidden. Only populated for video owner.
        /// </summary>
        public bool? IsHidden { get; set; }

        /// <summary>
        /// Whether the comment has been reported. Only populated for video owner.
        /// </summary>
        public bool? IsReported { get; set; }

        /// <summary>
        /// The reason the comment was reported. Only populated for video owner.
        /// </summary>
        public string? ReportedReason { get; set; }

        /// <summary>
        /// Whether the current user is the author of this comment.
        /// </summary>
        public bool IsOwn { get; set; }

        /// <summary>
        /// Whether the current user can moderate this comment (i.e., is the video owner).
        /// </summary>
        public bool CanModerate { get; set; }
    }
}
