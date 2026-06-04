namespace Application.Features.AccessManagement.Endpoints
{
    public class GetUserAccessResponse
    {
        public GetUserAccessSet Assigned { get; set; } = null!;
        public GetUserAccessSet Available { get; set; } = null!;
        public ListUserAccessPending Pending { get; set; } = null!;
    }
}