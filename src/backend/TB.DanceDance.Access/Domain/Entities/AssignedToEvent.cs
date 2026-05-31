using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TB.DanceDance.Access.Domain.Entities;

public class AssignedToEvent
{
    private AssignedToEvent() { } //for EF
    
    public Guid Id { get; set; }

    public required Guid EventId { get; init; }

    public required string UserId { get; init; }

    public Event Event { get; set; } = null!;
    
    public User User { get; set; } = null!;
    
    public class Factory
    {
        public static AssignedToEvent Create(Guid eventId, string userId)
        {
            return new AssignedToEvent
            {
                EventId = eventId,
                UserId = userId,
                Id = Guid.NewGuid(),
            };
        }
    }
}

public class AssignedToEventConfiguration : IEntityTypeConfiguration<AssignedToEvent>
{
    public void Configure(EntityTypeBuilder<AssignedToEvent> builder)
    {
        builder.ToTable("AssignedToEvents");
    }
}
