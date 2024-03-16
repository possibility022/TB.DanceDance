using System;

namespace TB.DanceDance.API.Contracts.Models
{
    public class RequestedAccess
    {
        public string Name { get; set; } = null!;
        public string RequestorFirstName { get; set; } = null!;
        public string RequestorLastName { get; set; } = null!;
        
        /// <summary>
        /// When joined to group. Required for group. Not required for event.
        /// </summary>
        public DateTime? WhenJoined { get; set; }
        public Guid RequestId { get; set; }

        /// <summary>
        /// When true - it is a group. Otherwise, event.
        /// </summary>
        public bool IsGroup { get; set; }
    }
}