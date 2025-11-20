namespace TB.DanceDance.Mobile.Library.Services.Network;

public record UploadProgressEvent()
{
    public required string FileName { get; init; }
    public required int SendBytes { get; init; }
    public required long FileSize { get; init; }
}