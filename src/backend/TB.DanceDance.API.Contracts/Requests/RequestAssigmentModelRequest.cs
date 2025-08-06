using System;
using System.Collections.Generic;

namespace TB.DanceDance.API.Contracts.Requests
{

    public class RequestAssigmentModelRequest
    {
        public ICollection<Guid>? Events { get; set; }
        public ICollection<GroupAssignmentModel>? Groups { get; set; }
    }

    public class GroupAssignmentModel
    {
        public Guid Id { get; set; }
        public DateTime JoinedDate { get; set; }
    }
}