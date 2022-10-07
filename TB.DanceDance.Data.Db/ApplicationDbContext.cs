using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Data.Models;

namespace TB.DanceDance.Data.Db
{
    public class ApplicationDbContext : DbContext
    {

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultContainer("dancedance");
            modelBuilder.Entity<VideoInformation>().HasPartitionKey(nameof(VideoInformation.PartitionKey));
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public ApplicationDbContext()
        {
            
        }

        public DbSet<VideoInformation> VideosInformation { get; set; }
    }
}
