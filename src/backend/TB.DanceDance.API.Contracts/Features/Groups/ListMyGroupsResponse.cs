using System;
using TB.DanceDance.API.Contracts.Features.Groups.Model;

namespace TB.DanceDance.API.Contracts.Features.Groups
{
    public class ListMyGroupsResponse
    {
        public GroupModel[] Groups { get; set; } = Array.Empty<GroupModel>();
    }
}
