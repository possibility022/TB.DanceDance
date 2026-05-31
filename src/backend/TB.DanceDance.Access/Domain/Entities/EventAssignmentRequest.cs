using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TB.DanceDance.Access.Domain.Entities;

public class EventAssignmentRequest : AssigmentRequestBase
{
    private EventAssignmentRequest() { }
    
    public Guid Id { get; set; }
    
    public required Guid EventId { get; init; }
    
    public class Factory
    {
        public static EventAssignmentRequest Create(string userId, Guid eventId)
        {
            return new EventAssignmentRequest
            {
                UserId = userId,
                EventId = eventId,
            };
        }
    }

}

public class EventAssignmentRequestConfiguration : IEntityTypeConfiguration<EventAssignmentRequest>
{
    public void Configure(EntityTypeBuilder<EventAssignmentRequest> builder)
    {
        builder.ToTable("EventAssignmentRequests");
        
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ManagedBy);
        
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .IsRequired();
    }
}
