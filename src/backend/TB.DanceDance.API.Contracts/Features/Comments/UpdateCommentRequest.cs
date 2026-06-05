namespace TB.DanceDance.API.Contracts.Features.Comments
{
    public class UpdateCommentRequest
    {
        /// <summary>The new comment content.</summary>
        public string Content { get; set; } = null!;

        /// <summary>Display name used when posting as anonymous.</summary>
        public string? AuthorName { get; set; }
    }
}