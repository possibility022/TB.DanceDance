using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TB.DanceDance.API.Contracts.Features.Videos;

namespace TB.DanceDance.Mobile.Library.Data.Models.Storage;

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

    public UploadJobState State { get; set; } = UploadJobState.PendingRegistration;
    public SharingWithType SharingWithType { get; set; } = SharingWithType.Private;
    public Guid? SharedWithId { get; set; }

    [MaxLength(200)]
    public string VideoName { get; set; } = string.Empty;

    public DateTime RecordedTimeUtc { get; set; }
    public long FileSize { get; set; }
    public long UploadedBytes { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }

    [MaxLength(2048)]
    public string? LastError { get; set; }

    public bool CancellationRequested { get; set; }
    public bool OwnsFile { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    [NotMapped]
    public double Progress => FileSize <= 0 ? 0 : Math.Clamp((double)UploadedBytes / FileSize, 0, 1);

    [NotMapped]
    public bool CanRetry => State is UploadJobState.Failed or UploadJobState.WaitingForAuthentication;

    [NotMapped]
    public bool CanCancel => State is not UploadJobState.Completed
        and not UploadJobState.Cancelled;
}