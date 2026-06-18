namespace TB.DanceDance.API.Contracts.Features.Transfers
{
    public class CreateTransferRequest
    {
        /// <summary>Number of days until the transfer link expires (1-365). Default 7.</summary>
        public int ExpirationDays { get; set; } = 7;
    }
}
