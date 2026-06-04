using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Features.AccessManagement.Models;

namespace TB.DanceDance.API.Contracts.Features.AccessManagement
{
    public class RequestAccessRequest
    {
        public ICollection<Guid>? Events { get; set; }
        public ICollection<RequestAccessGroupModel>? Groups { get; set; }
    }
}