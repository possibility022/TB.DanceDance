using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Domain.Entities;
using TB.DanceDance.SharedKernel;

namespace TB.DanceDance.Access.Infrastructure;

public class AccessDbContext : DbContext
{
    public DbSet<User> Users { get; set; }           // default schema
    public DbSet<Group> Groups { get; set; }          // access schema
    public DbSet<Event> Events { get; set; }          // access schema
    public DbSet<GroupAdmin> GroupsAdmins { get; set; }
    public DbSet<AssignedToGroup> AssignedToGroups { get; set; }
    public DbSet<AssignedToEvent> AssignedToEvents { get; set; }
    public DbSet<GroupAssignmentRequest> GroupAssignmentRequests { get; set; }
    public DbSet<EventAssignmentRequest> EventAssignmentRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GroupAssignmentRequest>()
            .ToTable(nameof(GroupAssignmentRequest), Constants.DbSchemas.Access);

        modelBuilder.Entity<EventAssignmentRequest>()
            .ToTable(nameof(EventAssignmentRequest), Constants.DbSchemas.Access);

        modelBuilder.Entity<Group>()
            .ToTable("Groups", Constants.DbSchemas.Access);

        modelBuilder.Entity<Event>()
            .ToTable("Events", Constants.DbSchemas.Access);

        modelBuilder.Entity<GroupAdmin>()
            .ToTable("GroupsAdmins", Constants.DbSchemas.Access);
        
        
        modelBuilder.Entity<Event>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.Owner)
            .IsRequired()
            ;

        modelBuilder.Entity<AssignedToGroup>()
            .ToTable(nameof(AssignedToGroups), Constants.DbSchemas.Access);

        modelBuilder.Entity<AssignedToEvent>()
            .ToTable(nameof(AssignedToEvents), Constants.DbSchemas.Access);


        modelBuilder.Entity<EventAssignmentRequest>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .IsRequired();

        modelBuilder.Entity<EventAssignmentRequest>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ManagedBy);

        modelBuilder.Entity<GroupAssignmentRequest>()
            .HasOne<Group>()
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .IsRequired();
        
        modelBuilder.Entity<GroupAssignmentRequest>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ManagedBy);
    }
}