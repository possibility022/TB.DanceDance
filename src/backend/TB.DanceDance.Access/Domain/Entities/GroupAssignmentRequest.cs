using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TB.DanceDance.Access.Domain.Entities;

public class GroupAssignmentRequest : AssigmentRequestBase
{
    private GroupAssignmentRequest() { }
    
    public Guid Id { get; set; }
    public required Guid GroupId { get; init; }
    public required DateTime WhenJoined { get; set; }
    
    public class Factory
    {
        public static GroupAssignmentRequest Create(string userId, Guid groupId, DateTime whenJoined)
        {
            return new GroupAssignmentRequest
            {
                UserId = userId,
                GroupId = groupId,
                WhenJoined = whenJoined,
                Id = Guid.NewGuid(),
            };
        }
    }
}

public class GroupAssignmentRequestConfiguration : IEntityTypeConfiguration<GroupAssignmentRequest>
{
    public void Configure(EntityTypeBuilder<GroupAssignmentRequest> builder)
    {
        builder.ToTable("GroupAssignmentRequests");
        
        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .IsRequired();
        
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ManagedBy);
    }
}