using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.Mobile.Data.VideoModels;

namespace TB.DanceDance.Mobile.Data;

public class VideosDbContext : DbContext
{
    public VideosDbContext(DbContextOptions<VideosDbContext> options) : base(options)
    {
        
    }

    public DbSet<LocalVideoUploadProgress> LocalVideoUploadProgresses { get; set; }
    public DbSet<LocalVideos> LocalVideos { get; set; }
}

public class LocalVideos
{
    [Key]
    public Guid VideoId { get; set; }
    public string VideoName { get; set; } = string.Empty;
    public bool Downloaded { get; set; }
}