using IdentityModel.OidcClient;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using TB.DanceDance.Mobile.Services.Auth;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.Services.Network;

public class HttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        if (name == nameof(DanceHttpApiClient))
            return ResolveClientForDanceApi();

        throw new ArgumentOutOfRangeException($"Client for name '{name}' not found.");
    }

    private static HttpClient? danceApiClient;

    private static HttpClient ResolveClientForDanceApi()
    {
        if (danceApiClient == null)
            InitializeDanceApiClient();

        return danceApiClient!;
    }

#if DEBUG
    private const string ApiUrl = "https://localhost:7068";
#else
    private const string ApiUrl = "https://localhost:7068";
#endif

    private static void InitializeDanceApiClient()
    {
        var retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions { BackoffType = DelayBackoffType.Exponential, MaxRetryAttempts = 3 })
            .Build();

        var socketHandler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
#if DEBUG
            SslOptions = new SslClientAuthenticationOptions()
            {
                RemoteCertificateValidationCallback = RemoteCertificateValidationCallback
            }
#endif
        };

        var tokenProvider = new TokenProviderService(new OidcClient(
            AuthSettingsFactory.GetClientOptions(socketHandler)
        ));

        var resilienceHandler =
            new TokenDelegatingHandler(tokenProvider, retryPipeline) { InnerHandler = socketHandler };

        AuthSettingsFactory.GetClientOptions(socketHandler);

        string apiUrl = NetworkAddressResolver.Resolve(ApiUrl);

        var httpClient = new HttpClient(resilienceHandler);
        httpClient.BaseAddress = new Uri(apiUrl);
        danceApiClient = httpClient;
    }

#if DEBUG
    private static bool RemoteCertificateValidationCallback(object message, X509Certificate? cert, X509Chain? chain,
        SslPolicyErrors errors)
    {
        if (cert is { Issuer: "CN=localhost" })
            return true;
        return errors == System.Net.Security.SslPolicyErrors.None;
    }
#endif
}