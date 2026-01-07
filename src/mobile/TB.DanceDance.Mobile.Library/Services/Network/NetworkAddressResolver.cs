using Microsoft.Maui.Devices;

namespace TB.DanceDance.Mobile.Library.Services.Network;

public class NetworkAddressResolver
{
    private readonly DevicePlatform platform;

    public NetworkAddressResolver(DevicePlatform platform)
    {
        this.platform = platform;
    }
    
#if DEBUG

    private const string Localhost = "localhost";
    private const string LoopAddress = "127.0.0.1";
    private const string AndroidHostMachine = "10.0.2.2";
    
#endif
    
    public Uri Resolve(Uri uri)
    {
        #if DEBUG

        if (platform == DevicePlatform.Android)
        {
            if (uri.Host.Equals(Localhost))
                return new UriBuilder(uri) { Host = AndroidHostMachine }.Uri;
            if (uri.Host.Equals(LoopAddress))
                return new UriBuilder(uri) { Host = AndroidHostMachine }.Uri;
        }
            
        #endif
        
        return uri;
    }
    
    public string Resolve(string uri)
    {
#if DEBUG

        if (platform == DevicePlatform.Android)
        {
            return uri.Replace(Localhost, AndroidHostMachine)
                .Replace(LoopAddress, AndroidHostMachine);
        }
#endif
        
        return uri;
    }
    
}