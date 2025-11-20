using Duende.IdentityModel.OidcClient.Browser;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Maui.Devices;
using Polly;
using Serilog;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using TB.DanceDance.Mobile.Library.Services.Auth;
using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.Library.Services.Network;

public interface IBrowserFactory
{
    IBrowser CreateBrowser();
}

public class HttpClientFactory : IHttpClientFactory
{
    private readonly IBrowserFactory browserFactory;
    private readonly TokenProviderService tokenProvider;
    private readonly DevicePlatform platform;
    private readonly NetworkAddressResolver networkAddressResolver;

    public HttpClientFactory(IBrowserFactory browserFactory,
        TokenProviderService tokenProvider,
        DevicePlatform platform)
    {
        this.browserFactory = browserFactory;
        this.tokenProvider = tokenProvider;
        this.platform = platform;
        this.networkAddressResolver = new NetworkAddressResolver(platform);
    }
    
    public HttpClient CreateClient(string name)
    {
        if (name == nameof(DanceHttpApiClient))
            return ResolveClientForDanceApi();

        throw new ArgumentOutOfRangeException($"Client for name '{name}' not found.");
    }

    private HttpClient? danceApiClient;

    private HttpClient ResolveClientForDanceApi()
    {
        var authSettingsFactory = new AuthSettingsFactory(browserFactory.CreateBrowser(), platform);
        
        if (danceApiClient == null)
            InitializeDanceApiClient(authSettingsFactory, tokenProvider);

        return danceApiClient!;
    }

    private static bool useBackupServer;

    public static async Task ValidatePrimaryHostIsAvailable(NetworkAddressResolver networkAddressResolver)
    {
        var socketHandler = CreateSocketHandler();
        var resilienceHandler = new ResilienceHandler(CreateRetryPipeline())
        {
            InnerHandler = socketHandler
        };
        
        using var httpClient = new HttpClient(resilienceHandler);
        try
        {
            var response = await httpClient.GetAsync(new Uri(networkAddressResolver.Resolve(ApiMainUrl) + KeysPath));
            if (response.IsSuccessStatusCode)
                useBackupServer = false;
        }
        catch (Exception e)
        {
            Log.Error(e, "An error occured while validating the primary host");
        }

        var responseFromBackup = await httpClient.GetAsync(new Uri(networkAddressResolver.Resolve(BackupUrl) + KeysPath));
        if (responseFromBackup.IsSuccessStatusCode)
        {
            useBackupServer = true;
        }
    }

    public static string ApiUrl => useBackupServer  ? BackupUrl : ApiMainUrl;

#if DEBUG
    private const string ApiMainUrl = "https://localhost:7068";
#else
    private const string ApiMainUrl = "https://ddapi.tomb.my.id";
#endif

    private const string BackupUrl = "https://wcsdance.azurewebsites.net";
    private const string KeysPath = "/.well-known/openid-configuration/jwks";

    private void InitializeDanceApiClient(AuthSettingsFactory factory, TokenProviderService tokenProvider)
    {
        var retryPipeline = CreateRetryPipeline();

        var socketHandler = CreateSocketHandler();

        var resilienceHandler =
            new TokenDelegatingHandler(tokenProvider, retryPipeline) { InnerHandler = socketHandler };

        factory.GetClientOptions(socketHandler);//todo - check this

        string apiUrl = networkAddressResolver.Resolve(ApiUrl);

        var httpClient = new HttpClient(resilienceHandler);
        httpClient.BaseAddress = new Uri(apiUrl);
        danceApiClient = httpClient;
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
}