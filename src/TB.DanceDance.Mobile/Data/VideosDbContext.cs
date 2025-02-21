using Microsoft.EntityFrameworkCore;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.Mobile.Data.VideoModels;

namespace TB.DanceDance.Mobile.Data;

public class VideosDbContext : DbContext
{
    public VideosDbContext(DbContextOptions<VideosDbContext> options) : base(options)
    {
        
    }

    public DbSet<LocalVideoUploadProgress> LocalVideoUploadProgresses { get; set; }
}