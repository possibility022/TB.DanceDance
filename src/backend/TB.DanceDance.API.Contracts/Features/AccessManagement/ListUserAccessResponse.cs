using TB.DanceDance.API.Contracts.Features.AccessManagement.Models;

namespace TB.DanceDance.API.Contracts.Features.AccessManagement
{
    public class GetUserAccessResponse
    {
        public GetUserAccessSet Assigned { get; set; } = null!;
        public GetUserAccessSet Available { get; set; } = null!;
        public ListUserAccessPending Pending { get; set; } = null!;
    }
}