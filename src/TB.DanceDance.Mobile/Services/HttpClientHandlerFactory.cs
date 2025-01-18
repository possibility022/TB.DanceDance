namespace TB.DanceDance.Mobile.Services;

public class HttpClientHandlerFactory
{
    public HttpClientHandler GetHttpClientHandler()
    {
#if DEBUG
        
        HttpClientHandler handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            if (cert.Issuer.Equals("CN=localhost"))
                return true;
            return errors == System.Net.Security.SslPolicyErrors.None;
        };
        return handler;
#endif
        return new HttpClientHandler();
    }
}