using System;
using System.Collections.Generic;

namespace Application.Features.AccessManagement.Endpoints
{
    public class RequestAccessRequest
    {
        public ICollection<Guid>? Events { get; set; }
        public ICollection<RequestAccessGroupModel>? Groups { get; set; }
    }
}