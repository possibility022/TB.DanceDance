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

public class BrowserFactory : IBrowserFactory
{
    Func<IBrowser> factory;

    public void SetFactory(Func<IBrowser> factory)
    {
        this.factory = factory;
    }

    public IBrowser CreateBrowser()
    {
        if (factory == null)
            throw new InvalidOperationException("Browser factory is not set.");
        return factory();
    }
}

public class HttpClientFactory : IHttpClientFactory
{
    private readonly ITokenProviderService tokenProvider;
    private readonly NetworkAddressResolver networkAddressResolver;
    private readonly AuthSettingsFactory authSettingsFactory;

    public HttpClientFactory(IBrowserFactory browserFactory,
        ITokenProviderService tokenProvider,
        NetworkAddressResolver networkAddressResolver,
        AuthSettingsFactory authSettingsFactory)
    {
        this.tokenProvider = tokenProvider;
        this.networkAddressResolver = networkAddressResolver;
        this.authSettingsFactory = authSettingsFactory;
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

            return;
        }
        catch (Exception e)
        {
            Log.Error(e, "An error occured while validating the primary host");
        }

        useBackupServer = true;

        try
        {
            var responseFromBackup = await httpClient.GetAsync(new Uri(networkAddressResolver.Resolve(BackupUrl) + KeysPath));
            if (responseFromBackup.IsSuccessStatusCode)
            {
                useBackupServer = true;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "An error occured while validating the backup host");
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

    private void InitializeDanceApiClient(AuthSettingsFactory factory, ITokenProviderService tokenProvider)
    {
        var retryPipeline = CreateRetryPipeline();

        var socketHandler = CreateSocketHandler();

        var resilienceHandler =
            new TokenDelegatingHandler(tokenProvider, retryPipeline) { InnerHandler = socketHandler };

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