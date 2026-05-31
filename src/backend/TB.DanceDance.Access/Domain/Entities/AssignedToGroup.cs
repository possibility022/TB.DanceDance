using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TB.DanceDance.Access.Domain.Entities;

public class AssignedToGroup
{
    private AssignedToGroup() { } //for EF
    
    public Guid Id { get; set; }

    public required Guid GroupId { get; init; }

    public required string UserId { get; init; }

    public required DateTime WhenJoined { get; set; }

    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
    
    public class Factory
    {
        public static AssignedToGroup Create(Guid groupId, string userId, DateTime whenJoined)
        {
            return new AssignedToGroup
            {
                GroupId = groupId,
                UserId = userId,
                WhenJoined = whenJoined
            };
        }
    }
}

public class AssignedToGroupConfiguration : IEntityTypeConfiguration<AssignedToGroup>
{
    public void Configure(EntityTypeBuilder<AssignedToGroup> builder)
    {
        builder.ToTable("AssignedToGroups");
    }
}
