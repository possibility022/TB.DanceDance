using Microsoft.Maui.Networking;

namespace TB.DanceDance.Mobile.Services.Network;

public class NetworkerSettings
{
    public bool UploadOnlyByWiFi { get; set; } = true;
}

public class NetworkStatusMonitor : IDisposable
{
    private static NetworkerSettings settings = new NetworkerSettings(); //todo, load from storage

    public static NetworkerSettings Settings
    {
        get => settings;
        set => settings = value;
    }

    public NetworkStatusMonitor()
    {
        Connectivity.ConnectivityChanged += ConnectivityOnConnectivityChanged;
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
    }

    void ConnectivityOnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        ManageBackgroundService(e.NetworkAccess, e.ConnectionProfiles);
    }

    ~NetworkStatusMonitor()
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