using System;

namespace TB.DanceDance.API.Contracts.Features.Groups.Model
{
    public class GroupMemberModel
    {
        public string UserId { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime WhenJoined { get; set; }
    }
}
