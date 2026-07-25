namespace TB.DanceDance.Mobile.Library.Data.Models.Storage;

public enum UploadJobState
{
    PendingRegistration = 0,
    PendingUpload = 1,
    Uploading = 2,
    WaitingForNetwork = 3,
    WaitingForAuthentication = 4,
    RetryScheduled = 5,
    Completed = 6,
    Failed = 7,
    Cancelled = 8
}
