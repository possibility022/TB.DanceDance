namespace Application.Features.AccessManagement.Endpoints
{
    public class GetUserAccessResponse
    {
        public GetUserAccessSet Assigned { get; set; } = null!;
        public GetUserAccessSet Available { get; set; } = null!;
        public GetUserAccessPending Pending { get; set; } = null!;
    }
}