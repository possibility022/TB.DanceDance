using System;

namespace Application.Features.AccessManagement.Endpoints
{
    public class RequestAccessGroupModel
    {
        public Guid Id { get; set; }
        public DateTime JoinedDate { get; set; }
    }
}