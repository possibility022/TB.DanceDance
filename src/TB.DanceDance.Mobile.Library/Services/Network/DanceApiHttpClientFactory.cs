using Duende.IdentityModel.OidcClient.Browser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using TB.DanceDance.Mobile.Library.Services.Auth;
using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.Library.Services.Network;

public interface IBrowserFactory
{
    IBrowser CreateBrowser();
}

public class DanceApiHttpClientFactory : IHttpClientFactory, IDisposable
{
    private readonly ITokenProviderService primaryTokenProvider;
    private readonly ITokenProviderService secondaryTokenProvider;
    private readonly NetworkAddressResolver networkAddressResolver;

    public DanceApiHttpClientFactory(
        [FromKeyedServices(TokenStorage.PrimaryStorageKey)] ITokenProviderService primaryTokenProvider,
        [FromKeyedServices(TokenStorage.SecondaryStorageKey)] ITokenProviderService secondaryTokenProvider,
        
        NetworkAddressResolver networkAddressResolver)
    {
        this.primaryTokenProvider = primaryTokenProvider;
        this.secondaryTokenProvider = secondaryTokenProvider;
        this.networkAddressResolver = networkAddressResolver;
    }
    
    public HttpClient CreateClient(string name)
    {
        if (name == nameof(DanceHttpApiClient))
            return ResolveClientForDanceApi();

        throw new ArgumentOutOfRangeException($"Client for name '{name}' not found.");
    }

    private HttpClient? danceApiClient;

#if DEBUG
    public const string ApiMainUrl = "https://localhost:7068";
#else
    public const string ApiMainUrl = "https://ddapi.tomb.my.id";
#endif

    public const string ApiBackupUrl = "https://localhost:7068";
    private const string KeysPath = "/.well-known/openid-configuration/jwks";

    private HttpClient ResolveClientForDanceApi()
    {       
        danceApiClient ??= InitializeDanceApiClient();
        return danceApiClient;
    }
    
    private HttpClient InitializeDanceApiClient()
    {
        var handlerChain = CreateHandlersChainWithAuthTokenProviderChained();

        var httpClient = new HttpClient(handlerChain);
        httpClient.BaseAddress = new Uri(ApiMainUrl);
        return httpClient;
    }

    public static HttpMessageHandler CreateBaseHttpMessageHandlerChain(NetworkAddressResolver networkAddressResolver)
    {
        HttpMessageHandler innerHandler = CreateSocketHandler();

#if DEBUG
        innerHandler = new DebuggingUrlHandler(networkAddressResolver, innerHandler);
#endif

        return innerHandler;
    }

    public HttpMessageHandler CreateBaseHttpMessageHandlerChain() => CreateBaseHttpMessageHandlerChain(this.networkAddressResolver);

    private HttpMessageHandler CreateHandlersChainWithAuthTokenProviderChained()
    {
        var retryPipeline = CreateRetryPipeline();
        
        var resilienceHandler = new ResilienceHandler(retryPipeline){ InnerHandler = CreateSocketHandler() };

        HttpMessageHandler innerHandler =
            new TokenDelegatingHandler(primaryTokenProvider, secondaryTokenProvider) { InnerHandler = resilienceHandler };

#if DEBUG
        innerHandler = new DebuggingUrlHandler(networkAddressResolver, innerHandler);
#endif

        var baseMessageHandlerForHealthEndpoints = CreateBaseHttpMessageHandlerChain(networkAddressResolver);
        var backupServerHandler = CreateBackupServerHttpHandler(innerHandler);

        return backupServerHandler;
    }

    private BackupServerHttpHandler CreateBackupServerHttpHandler(HttpMessageHandler innerHandler)
    {
        return new BackupServerHttpHandler(new ServersConfiguration()
        {
            HealthEndpoint = KeysPath,
            Primary = new Uri(ApiMainUrl),
            Secondary = new Uri(ApiBackupUrl),
        }, innerHandler, CreateBaseHttpMessageHandlerChain);
    }

    private static ResiliencePipeline<HttpResponseMessage> CreateRetryPipeline()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions { BackoffType = DelayBackoffType.Exponential, MaxRetryAttempts = 3 })
            .Build();
    }

    public static SocketsHttpHandler CreateSocketHandler()
    {
        return new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            ConnectTimeout = TimeSpan.FromSeconds(5),
#if DEBUG
            SslOptions = new SslClientAuthenticationOptions()
            {
                RemoteCertificateValidationCallback = RemoteCertificateValidationCallback
            }
#endif
        };
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

    public void Dispose()
    {
        danceApiClient?.Dispose();
    }
}