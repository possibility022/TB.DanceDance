using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application;
public interface IApplicationContext
{
    DbSet<Video> Videos { get; }
    DbSet<VideoMetadata> VideoMetadata { get; }
    DbSet<GroupAssigmentRequest> GroupAssigmentRequests { get; }
    DbSet<EventAssigmentRequest> EventAssigmentRequests { get; }
    DbSet<SharedWith> SharedWith { get; }
    DbSet<Group> Groups { get; }
    DbSet<Event> Events { get; }
    DbSet<AssignedToGroup> AssingedToGroups { get; }
    DbSet<AssignedToEvent> AssingedToEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}
