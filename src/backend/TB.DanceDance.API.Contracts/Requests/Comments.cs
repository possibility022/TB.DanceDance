using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.API.Contracts.Requests
{
    public abstract class BaseComment
    {
        /// <summary>
        /// The comment content.
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = null!;
        
        /// <summary>
        /// Anonymous id that is stored on the client side. Allows updating comments posted anonymously.
        /// </summary>
        [MaxLength(1000)]
        public string? AnonymousId { get; set; } = null!;
        
        /// <summary>
        /// Author if posting as anonymous.
        /// </summary>
        [MaxLength(20)]
        public string? AuthorName { get; set; }
    }
    
    public class UpdateCommentRequest : BaseComment
    {

    }
    
    public class CreateCommentRequest : BaseComment
    {
        
    }
    
    public class ReportCommentRequest
    {
        /// <summary>
        /// The reason for reporting this comment.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = null!;
    }
}