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

        DbSet<Video> Videos { get; set; }
        DbSet<GroupAssigmentRequest> GroupAssigmentRequests { get; set; }
        DbSet<EventAssigmentRequest> EventAssigmentRequests { get; set; }
        DbSet<SharedWith> SharedWith { get; set; }
        DbSet<Group> Groups { get; set; }
        DbSet<Event> Events { get; set; }

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
