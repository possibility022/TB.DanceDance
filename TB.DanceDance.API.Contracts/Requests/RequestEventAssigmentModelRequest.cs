using System;
using System.Collections.Generic;

namespace TB.DanceDance.API.Contracts.Requests
{

    public class RequestEventAssigmentModelRequest
    {
        public ICollection<Guid>? Events { get; set; }
        public ICollection<Guid>? Groups { get; set; }
    }
}