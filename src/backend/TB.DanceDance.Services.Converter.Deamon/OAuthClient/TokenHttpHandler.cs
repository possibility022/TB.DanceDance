namespace TB.DanceDance.Services.Converter.Deamon.OAuthClient;
internal class TokenHttpHandler : DelegatingHandler
{
    private readonly TokenProvider tokenProvider;

    public TokenHttpHandler(TokenProvider tokenProvider) : base()
    {
        this.tokenProvider = tokenProvider;
        this.InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(token.Schema, token.AccessToken);

        return await base.SendAsync(request, cancellationToken);
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}
