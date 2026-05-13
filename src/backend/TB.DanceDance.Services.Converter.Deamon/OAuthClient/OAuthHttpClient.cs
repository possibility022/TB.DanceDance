namespace TB.DanceDance.Services.Converter.Deamon.OAuthClient;
public class OAuthHttpClient : HttpClient
{
    public OAuthHttpClient() { }
    public OAuthHttpClient(HttpMessageHandler handler) : base(handler) { }
}
