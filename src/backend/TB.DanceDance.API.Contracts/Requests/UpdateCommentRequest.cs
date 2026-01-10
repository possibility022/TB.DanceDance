using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.API.Contracts.Requests
{
    public class UpdateCommentRequest
    {
        /// <summary>
        /// The updated comment content.
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = null!;

        /// <summary>
        /// Anonymouse id that is stored on the client side. Allows updating comments posted anonymously.
        /// </summary>
        [MaxLength(1000)]
        public string? AnonymouseId { get; set; } = null!;
    }
}
