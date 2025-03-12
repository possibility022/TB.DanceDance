using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.Mobile.Data.VideoModels;

public class LocalVideoUploadProgress
{
    [Key] public Guid Id { get; set; }
    public string FullFileName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool Uploaded { get; set; }
    public Guid RemoteVideoId { get; set; }
    public string Sas { get; set; } = string.Empty;
    public DateTime SasExpireAt { get; set; }
}