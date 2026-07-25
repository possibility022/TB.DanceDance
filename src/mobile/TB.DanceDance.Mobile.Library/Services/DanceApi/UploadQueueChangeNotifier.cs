namespace TB.DanceDance.Mobile.Library.Services.DanceApi;

public interface IUploadQueueChangeNotifier
{
    event EventHandler? Changed;
    void NotifyChanged();
}

public sealed class UploadQueueChangeNotifier : IUploadQueueChangeNotifier
{
    public event EventHandler? Changed;

    public void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
