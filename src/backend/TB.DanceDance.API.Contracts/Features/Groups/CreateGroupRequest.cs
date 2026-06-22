using System;

namespace TB.DanceDance.API.Contracts.Features.Groups
{
    public class CreateGroupRequest
    {
        public string Name { get; set; } = null!;
        public DateTime SeasonStart { get; set; }
        public DateTime SeasonEnd { get; set; }
    }
}
