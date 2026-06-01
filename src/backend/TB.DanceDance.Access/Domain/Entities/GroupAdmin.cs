using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TB.DanceDance.Access.Domain.Entities;
public class GroupAdmin
{
    private GroupAdmin() { }
    
    public required Guid Id { get; set; }
    public required string UserId { get; set; }
    public required Guid GroupId { get; set; }

    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;

    public class Factory
    {
        public static GroupAdmin Create(string userId, Guid groupId)
        {
            return new GroupAdmin
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GroupId = groupId,
            };
        }
    }
}

public class GroupAdminConfiguration : IEntityTypeConfiguration<GroupAdmin>
{
    public void Configure(EntityTypeBuilder<GroupAdmin> builder)
    {
        builder.ToTable("GroupsAdmins");
    }
}