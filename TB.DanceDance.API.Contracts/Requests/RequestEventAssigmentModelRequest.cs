using System;
using System.Collections.Generic;

namespace TB.DanceDance.API.Contracts.Requests
{

    public class RequestEventAssigmentModelRequest
    {
        public ICollection<Guid>? Events { get; set; }
        public ICollection<GroupAssigmentModel>? Groups { get; set; }
    }

    public class GroupAssigmentModel
    {
        public Guid Id { get; set; }
        public DateTime JoinedDate { get; set; }
    }
}