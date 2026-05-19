using System;

namespace TB.DanceDance.API.Contracts.Features.AccessManagement
{
    public class ApproveAccessRequest
    {
        public Guid RequestId { get; set; }
        public bool IsGroup { get; set; }
        public bool IsApproved { get; set; }
    }
}
