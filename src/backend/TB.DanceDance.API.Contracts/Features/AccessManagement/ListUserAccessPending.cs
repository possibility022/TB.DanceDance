using System;
using System.Collections.Generic;

namespace Application.Features.AccessManagement.Endpoints
{
    public class ListUserAccessPending
    {
        public  IReadOnlyCollection<Guid> Events { get; set; } = Array.Empty<Guid>();
        public  IReadOnlyCollection<Guid> Groups { get; set; } = Array.Empty<Guid>();
    }
}