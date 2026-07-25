using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.Pages.Upload;

public partial class UploadManagerPage : ContentPage
{
    private readonly UploadManagerPageModel model;
    private readonly IDispatcherTimer refreshTimer;
    private readonly IUploadQueueChangeNotifier changeNotifier;

    public UploadManagerPage(
        UploadManagerPageModel model,
        IUploadQueueChangeNotifier changeNotifier)
    {
        this.model = model;
        this.changeNotifier = changeNotifier;
        BindingContext = model;
        InitializeComponent();

        refreshTimer = Dispatcher.CreateTimer();
        refreshTimer.Interval = TimeSpan.FromSeconds(5);
        refreshTimer.Tick += RefreshTimerOnTick;
        Appearing += OnAppearing;
        Disappearing += OnDisappearing;
    }

    private void OnAppearing(object? sender, EventArgs e)
    {
        changeNotifier.Changed -= UploadQueueChanged;
        changeNotifier.Changed += UploadQueueChanged;
        refreshTimer.Start();
    }

    private void OnDisappearing(object? sender, EventArgs e)
    {
        refreshTimer.Stop();
        changeNotifier.Changed -= UploadQueueChanged;
    }

    private void UploadQueueChanged(object? sender, EventArgs e) =>
        Dispatcher.Dispatch(() => _ = RefreshSafelyAsync());

    private async void RefreshTimerOnTick(object? sender, EventArgs e)
        => await RefreshSafelyAsync();

    private async Task RefreshSafelyAsync()
    {
        try
        {
            await model.RefreshLiveAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Debug(ex, "Could not refresh upload queue status.");
        }
    }
}