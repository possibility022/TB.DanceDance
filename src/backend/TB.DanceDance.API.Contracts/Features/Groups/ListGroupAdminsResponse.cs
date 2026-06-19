using System;
using TB.DanceDance.API.Contracts.Features.Groups.Model;

namespace TB.DanceDance.API.Contracts.Features.Groups
{
    public class ListGroupAdminsResponse
    {
        public GroupAdminModel[] Admins { get; set; } = Array.Empty<GroupAdminModel>();
    }
}
