using System;
using System.Collections.Generic;
using TB.DanceDance.API.Contracts.Features.Videos;

namespace TB.DanceDance.API.Contracts.Features.Groups
{
    public class GroupWithVideosResponse
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = null!;
        public DateTime SeasonStart { get; set; }
        public DateTime SeasonEnd { get; set; }
        public ICollection<VideoInformationModel> Videos { get; set; } = null!;
    }
}
