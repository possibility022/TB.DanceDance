using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.API.Contracts.Requests
{
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
