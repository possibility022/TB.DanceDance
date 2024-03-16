using System;

namespace TB.DanceDance.API.Contracts.Requests
{
    public class ApproveAccessRequest
    {
        public Guid RequestId { get; set; }
        public bool IsGroup { get; set; }
        public bool IsApproved { get; set; }
    }
}
