using System;
using System.Collections.Generic;

namespace TB.DanceDance.API.Contracts.Features.Transfers
{
    public class CreateTransferRequest
    {
        /// <summary>The ids of the videos to transfer. Must be owned, converted, and private.</summary>
        public IReadOnlyCollection<Guid> VideoIds { get; set; } = Array.Empty<Guid>();

        /// <summary>Number of days until the transfer link expires (1-365). Default 7.</summary>
        public int ExpirationDays { get; set; } = 7;
    }
}
