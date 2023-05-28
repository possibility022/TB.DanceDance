using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.Data.PostgreSQL
{
    public class DanceDbContext : DbContext
    {
        public DanceDbContext(DbContextOptions<DanceDbContext> dbContextOptions) : base(dbContextOptions)
        {
            
        }

        public DbSet<Video> Videos { get; set; }
        public DbSet<GroupAssigmentRequest> GroupAssigmentRequests { get; set; }
        public DbSet<EventAssigmentRequest> EventAssigmentRequests { get; set; }
        public DbSet<SharedWith> SharedWith { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<AssignedToGroup> AssingedToGroups { get; set; }
        public DbSet<AssignedToEvent> AssingedToEvents { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EventAssigmentRequest>()
                .HasOne<Event>()
                .WithMany()
                .HasForeignKey(e => e.EventId)
                .IsRequired();

            modelBuilder.Entity<GroupAssigmentRequest>()
                .HasOne<Group>()
                .WithMany()
                .HasForeignKey(e => e.GroupId)
                .IsRequired();
        }

    }
}
