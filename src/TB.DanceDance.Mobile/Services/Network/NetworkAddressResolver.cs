namespace TB.DanceDance.Mobile.Services.Network;

public static class NetworkAddressResolver
{
    
#if DEBUG

    private const string Localhost = "localhost";
    private const string LoopAddress = "127.0.0.1";
    private const string AndroidHostMachine = "10.0.2.2";
    
#endif
    
    public static Uri Resolve(Uri uri)
    {
        #if DEBUG

        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            if (uri.Host.Equals(Localhost))
                return new UriBuilder(uri) { Host = AndroidHostMachine }.Uri;
            if (uri.Host.Equals(LoopAddress))
                return new UriBuilder(uri) { Host = AndroidHostMachine }.Uri;
        }
            
        #endif
        
        return uri;
    }
    
    public static string Resolve(string uri)
    {
#if DEBUG

        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            return uri.Replace(Localhost, AndroidHostMachine)
                .Replace(LoopAddress, AndroidHostMachine);
        }
#endif
        
        return uri;
    }
    
}