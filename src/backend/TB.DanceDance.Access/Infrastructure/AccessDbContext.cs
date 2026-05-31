using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Domain.Entities;

namespace TB.DanceDance.Access.Infrastructure;

internal static class SchemaNames
{
    public const string Access = "access";
}

public class AccessDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<GroupAdmin> GroupsAdmins { get; set; }
    public DbSet<AssignedToGroup> AssignedToGroups { get; set; }
    public DbSet<AssignedToEvent> AssignedToEvents { get; set; }
    public DbSet<GroupAssignmentRequest> GroupAssignmentRequests { get; set; }
    public DbSet<EventAssignmentRequest> EventAssignmentRequests { get; set; }

    public AccessDbContext(DbContextOptions<AccessDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccessDbContext).Assembly);
        modelBuilder.HasDefaultSchema(SchemaNames.Access);
    }
}