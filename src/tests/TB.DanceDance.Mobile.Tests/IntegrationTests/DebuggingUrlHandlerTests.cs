using Microsoft.Maui.Devices;
using TB.DanceDance.Mobile.Library.Services.Network;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class DebuggingUrlHandlerTests
{
    private class CapturingHandler : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            });
        }

#if NET9_0_OR_GREATER
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            };
        }
#endif
    }

    private static (HttpClient client, CapturingHandler inner) CreateClient(DevicePlatform platform)
    {
        var resolver = new NetworkAddressResolver(platform);
        var inner = new CapturingHandler();
        var handler = new DebuggingUrlHandler(resolver, inner);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };
        return (client, inner);
    }

    [Fact]
    public async Task Resolve_Localhost_OnAndroid_RewritesToEmulatorHost()
    {
        var (client, inner) = CreateClient(DevicePlatform.Android);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        Assert.NotNull(inner.LastRequestUri);
        Assert.Equal("10.0.2.2", inner.LastRequestUri!.Host);
        Assert.Equal(5000, inner.LastRequestUri!.Port);
        Assert.Equal("/api/test", inner.LastRequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Resolve_Loopback_OnAndroid_RewritesToEmulatorHost()
    {
        var resolver = new NetworkAddressResolver(DevicePlatform.Android);
        var inner = new CapturingHandler();
        var handler = new DebuggingUrlHandler(resolver, inner);
        var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1:8080/health");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        Assert.NotNull(inner.LastRequestUri);
        Assert.Equal("10.0.2.2", inner.LastRequestUri!.Host);
        Assert.Equal(8080, inner.LastRequestUri!.Port);
        Assert.Equal("/health", inner.LastRequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Resolve_OnNonAndroid_NoChange()
    {
        var (client, inner) = CreateClient(DevicePlatform.WinUI);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        Assert.NotNull(inner.LastRequestUri);
        Assert.Equal("localhost", inner.LastRequestUri!.Host);
        Assert.Equal(5000, inner.LastRequestUri!.Port);
        Assert.Equal("/api/test", inner.LastRequestUri!.AbsolutePath);
    }

#if NET9_0_OR_GREATER
    [Fact]
    public void Sync_Send_Rewrites_Too()
    {
        var resolver = new NetworkAddressResolver(DevicePlatform.Android);
        var inner = new CapturingHandler();
        var handler = new DebuggingUrlHandler(resolver, inner);

        using var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:1234/a");
        var response = invoker.Send(request, TestContext.Current.CancellationToken);
        Assert.True(response.IsSuccessStatusCode);

        Assert.NotNull(inner.LastRequestUri);
        Assert.Equal("10.0.2.2", inner.LastRequestUri!.Host);
        Assert.Equal(1234, inner.LastRequestUri!.Port);
        Assert.Equal("/a", inner.LastRequestUri!.AbsolutePath);
    }
#endif
}