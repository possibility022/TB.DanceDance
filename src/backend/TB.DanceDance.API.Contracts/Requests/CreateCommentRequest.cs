using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.API.Contracts.Requests
{
    public class CreateCommentRequest
    {
        /// <summary>
        /// The comment content.
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = null!;
    }
}
