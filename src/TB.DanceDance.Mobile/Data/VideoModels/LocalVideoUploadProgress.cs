using System.ComponentModel.DataAnnotations;

namespace TB.DanceDance.Mobile.Data.VideoModels;

public class LocalVideoUploadProgress
{
    [Key]
    public string FullFileName { get; set; }
    public string Filename { get; set; } = null!;
    public bool Uploaded { get; set; }
    public ushort Progress { get; set; }
}