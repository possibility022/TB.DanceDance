using System;

namespace TB.DanceDance.API.Contracts.Models
{
    public class Group
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime SeasonStart { get; set; }
        public DateTime SeasonEnd { get; set; }
    }
}
