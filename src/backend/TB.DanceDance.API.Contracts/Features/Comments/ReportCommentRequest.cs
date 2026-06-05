namespace TB.DanceDance.API.Contracts.Features.Comments
{
    public class ReportCommentRequest
    {
        /// <summary>The reason for reporting this comment.</summary>
        public string Reason { get; set; } = null!;
    }
}