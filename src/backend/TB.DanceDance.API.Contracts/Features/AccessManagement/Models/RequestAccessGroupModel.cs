using System;

namespace TB.DanceDance.API.Contracts.Features.AccessManagement.Models
{
    public class RequestAccessGroupModel
    {
        public Guid Id { get; set; }
        public DateTime JoinedDate { get; set; }
    }
}