using System;

namespace Application.Features.AccessManagement.Endpoints
{
    public class ApproveAccessRequestRequest
    {
        public Guid RequestId { get; set; }
        public bool IsGroup { get; set; }
        public bool IsApproved { get; set; }
    }
}