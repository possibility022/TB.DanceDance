namespace TB.DanceDance.Mobile;

public partial class MainPage : ContentPage
{
    int count = 0;

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
#if ANDROID
        try
        {
            UploadForegroundService.StartService();
            //Platform.CurrentActivity?.StartForegroundServiceCompat<UploadForegroundService>();
        }
        catch (Exception ex)
        {
            ErrorDetails.Text = ex.ToString();
        }
#endif

        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }
}