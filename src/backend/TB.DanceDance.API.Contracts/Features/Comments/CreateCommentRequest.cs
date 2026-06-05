namespace TB.DanceDance.API.Contracts.Features.Comments
{
    public class CreateCommentRequest
    {
        /// <summary>The comment content.</summary>
        public string Content { get; set; } = null!;

        /// <summary>Client-side anonymous id, allowing anonymous authors to edit their comment later.</summary>
        public string? AnonymousId { get; set; }

        /// <summary>Display name used when posting as anonymous.</summary>
        public string? AuthorName { get; set; }
    }
}