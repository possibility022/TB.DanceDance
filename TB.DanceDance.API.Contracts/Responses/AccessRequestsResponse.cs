using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.API.Contracts.Responses
{
    public class AccessRequestsResponse
    {
        public IReadOnlyCollection<AccessRequest> AccessRequests { get; set; } = Array.Empty<AccessRequest>();
    }
}
