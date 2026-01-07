namespace TB.DanceDance.Mobile.Library.Services.Network;

public class DebuggingUrlHandler : DelegatingHandler
{
    private readonly NetworkAddressResolver networkAddressResolver;

    public DebuggingUrlHandler(NetworkAddressResolver networkAddressResolver, HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        this.networkAddressResolver = networkAddressResolver;
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        OverwriteUri(request);

        return base.Send(request, cancellationToken);
    }

    private void OverwriteUri(HttpRequestMessage request)
    {
        if (request.RequestUri is not null)
            request.RequestUri = networkAddressResolver.Resolve(request.RequestUri);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        OverwriteUri(request);
        return base.SendAsync(request, cancellationToken);
    }
}