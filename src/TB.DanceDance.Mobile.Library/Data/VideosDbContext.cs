using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Mobile.Data.Models.Storage;

namespace TB.DanceDance.Mobile.Data;

public class VideosDbContext : DbContext
{
    public VideosDbContext(DbContextOptions<VideosDbContext> options) : base(options)
    {

    }

    public DbSet<VideosToUpload> VideosToUpload { get; set; }
}