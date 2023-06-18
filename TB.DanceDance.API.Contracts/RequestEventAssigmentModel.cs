using System;
using System.Collections.Generic;

namespace TB.DanceDance.API.Contracts
{

    public class RequestEventAssigmentModel
    {
        public ICollection<Guid>? Events { get; set; }
        public ICollection<Guid>? Groups { get; set; }
    }
}