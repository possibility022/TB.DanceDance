using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.Mobile.Data.Models.Storage;

public class VideosToUpload
{
    [Key] public Guid Id { get; set; }
    
    [MaxLength(512)]
    public string FullFileName { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string FileName { get; set; } = string.Empty;
    public bool Uploaded { get; set; }
    public Guid RemoteVideoId { get; set; }
    
    [MaxLength(1024)]
    public string Sas { get; set; } = string.Empty;
    public DateTime SasExpireAt { get; set; }
}