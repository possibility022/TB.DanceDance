namespace TB.DanceDance.Mobile.Pages.Upload;

public partial class UploadManagerPage : ContentPage
{
    private readonly UploadManagerPageModel model;
    private readonly IDispatcherTimer refreshTimer;

    public UploadManagerPage(UploadManagerPageModel model)
    {
        this.model = model;
        BindingContext = model;
        InitializeComponent();

        refreshTimer = Dispatcher.CreateTimer();
        refreshTimer.Interval = TimeSpan.FromSeconds(1);
        refreshTimer.Tick += RefreshTimerOnTick;
        Appearing += (_, _) => refreshTimer.Start();
        Disappearing += (_, _) => refreshTimer.Stop();
    }

    private async void RefreshTimerOnTick(object? sender, EventArgs e)
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