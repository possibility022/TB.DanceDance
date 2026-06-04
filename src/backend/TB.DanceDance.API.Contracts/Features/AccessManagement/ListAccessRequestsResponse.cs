using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Models;

namespace Application.Features.AccessManagement.Endpoints
{
    public class ListAccessRequestsResponse
    {
        public IReadOnlyCollection<RequestedAccessModel> AccessRequests { get; set; } = Array.Empty<RequestedAccessModel>();
    }
}