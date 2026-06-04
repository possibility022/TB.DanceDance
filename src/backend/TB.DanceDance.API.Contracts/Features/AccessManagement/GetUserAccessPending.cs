using System;
using System.Collections.Generic;

namespace Application.Features.AccessManagement.Endpoints
{
    public class GetUserAccessPending
    {
        public  IReadOnlyCollection<Guid> Events { get; set; } = Array.Empty<Guid>();
        public  IReadOnlyCollection<Guid> Groups { get; set; } = Array.Empty<Guid>();
    }
}