using System;
using System.Collections.Generic;

namespace TB.DanceDance.API.Contracts.Features.AccessManagement
{
    public class ListUserAccessPending
    {
        public  IReadOnlyCollection<Guid> Events { get; set; } = Array.Empty<Guid>();
        public  IReadOnlyCollection<Guid> Groups { get; set; } = Array.Empty<Guid>();
    }
}