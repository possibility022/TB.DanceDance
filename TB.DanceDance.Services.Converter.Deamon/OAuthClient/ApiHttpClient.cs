namespace TB.DanceDance.Services.Converter.Deamon.OAuthClient;
internal class ApiHttpClient : HttpClient
{
    public ApiHttpClient(HttpMessageHandler handler) : base(handler) { }
    public ApiHttpClient(HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler) { }
}
