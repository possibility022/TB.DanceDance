using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Domain.Entities;

namespace TB.DanceDance.Access.Mappers;

internal static class GroupMappers
{
    extension(Group group)
    {
        internal AssignedGroupDto MapToAssignedGroupDto(DateTime whenJoined)
        {
            return new AssignedGroupDto()
            {
                Id = group.Id,
                Name = group.Name,
                SeasonStart = group.SeasonStart,
                SeasonEnd = group.SeasonEnd,
                WhenJoined = whenJoined,
            };
        }
        
        internal GroupDto MapToDto()
        {
            return new GroupDto()
            {
                Id = group.Id,
                Name = group.Name,
                SeasonStart = group.SeasonStart,
                SeasonEnd = group.SeasonEnd,
            };
        }
    }
}