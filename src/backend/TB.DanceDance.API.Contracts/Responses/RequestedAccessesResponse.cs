using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.API.Contracts.Responses
{
    public class RequestedAccessesResponse
    {
        public IReadOnlyCollection<RequestedAccess> AccessRequests { get; set; } = Array.Empty<RequestedAccess>();
    }
}
