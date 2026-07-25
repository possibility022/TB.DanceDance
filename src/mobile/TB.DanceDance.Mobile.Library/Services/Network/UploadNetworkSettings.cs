using Microsoft.Maui.Storage;

namespace TB.DanceDance.Mobile.Library.Services.Network;

public interface IUploadNetworkSettings
{
    bool UploadOnlyOnWiFi { get; set; }
}

public sealed class UploadNetworkSettings : IUploadNetworkSettings
{
    private const string UploadOnlyOnWiFiKey = "uploads.wifi-only";

    public bool UploadOnlyOnWiFi
    {
        get => Preferences.Default.Get(UploadOnlyOnWiFiKey, true);
        set => Preferences.Default.Set(UploadOnlyOnWiFiKey, value);
    }
}
