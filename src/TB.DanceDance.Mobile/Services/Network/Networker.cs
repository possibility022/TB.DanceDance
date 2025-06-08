namespace TB.DanceDance.Mobile.Services.Network;

public class NetworkerSettings
{
    public bool UploadOnlyByWiFi { get; set; } = true;
}

public class Networker : IDisposable
{
    private static NetworkerSettings settings = new NetworkerSettings(); //todo, load from storage

    public static NetworkerSettings Settings
    {
        get => settings;
        set => settings = value;
    }

    private Task runBackgroundServiceTask; 

    public Networker()
    {
        runBackgroundServiceTask = StartNewTask();
        Connectivity.ConnectivityChanged += ConnectivityOnConnectivityChanged;
    }

    private Task StartNewTask()
    {
        return Task.Run(() =>
        {
            try
            {
                ManageBackgroundService(Connectivity.Current.NetworkAccess, Connectivity.ConnectionProfiles);
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, "Networker");
            }
        });
    }

    private void ManageBackgroundService(NetworkAccess access, IEnumerable<ConnectionProfile> connectionProfiles)
    {
        if (access == NetworkAccess.Internet)
        {
            if (Settings.UploadOnlyByWiFi && connectionProfiles.Contains(ConnectionProfile.WiFi))
            {
                Serilog.Log.Information("Background service started");
#if ANDROID
                UploadForegroundService.StartService();
#endif
                return;
            }
            else if (!Settings.UploadOnlyByWiFi)
            {
                Serilog.Log.Information("Background service started");
#if ANDROID
                UploadForegroundService.StartService();
#endif
                return;
            }
        }
        else
        {
            Serilog.Log.Information("Background service stopped");
#if ANDROID
            UploadForegroundService.StopService();
#endif
        }
    }

    void ConnectivityOnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        ManageBackgroundService(e.NetworkAccess, e.ConnectionProfiles);
    }

    ~Networker()
    {
        ReleaseUnmanagedResources();
    }

    private void ReleaseUnmanagedResources()
    {
        Connectivity.ConnectivityChanged -= ConnectivityOnConnectivityChanged;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}