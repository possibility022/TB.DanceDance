using System;
using TB.DanceDance.API.Contracts.Features.Groups.Model;

namespace TB.DanceDance.API.Contracts.Features.Groups
{
    public class ListGroupMembersResponse
    {
        public GroupMemberModel[] Members { get; set; } = Array.Empty<GroupMemberModel>();
    }
}
