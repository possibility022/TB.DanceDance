using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Features.AccessManagement.Models;
using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.API.Contracts.Features.AccessManagement
{
    public class ListAccessRequestsResponse
    {
        public IReadOnlyCollection<RequestedAccessModel> AccessRequests { get; set; } = Array.Empty<RequestedAccessModel>();
    }
}