using System;

namespace TB.DanceDance.API.Contracts.Models
{
    public class AccessRequest
    {
        public string Name { get; set; } = null!;
        public string RequestorFirstName { get; set; } = null!;
        public string RequestorLastName { get; set; } = null!;
        public Guid RequestId { get; set; }

        /// <summary>
        /// When true - it is a group. Otherwise, event.
        /// </summary>
        public bool IsGroup { get; set; }
    }
}