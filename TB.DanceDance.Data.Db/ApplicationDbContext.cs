using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Data.Models;

namespace TB.DanceDance.Data.Db
{
    public class ApplicationDbContext : DbContext
    {

        // Quick workaround
        public static string? ConnectionString = null;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            if (ConnectionString != null)
                optionsBuilder.UseSqlServer(ConnectionString);
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
