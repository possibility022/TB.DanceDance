using System;

namespace TB.DanceDance.API.Contracts
{
    public class Group
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
