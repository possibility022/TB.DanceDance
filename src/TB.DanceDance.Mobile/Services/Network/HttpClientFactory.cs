using IdentityModel.OidcClient;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Serilog;
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

    private static bool useBackupServer;

    public static async Task ValidatePrimaryHostIsAvailable()
    {
        var socketHandler = CreateSocketHandler();
        var resilienceHandler = new ResilienceHandler(CreateRetryPipeline())
        {
            InnerHandler = socketHandler
        };
        
        using var httpClient = new HttpClient(resilienceHandler);
        try
        {
            var response = await httpClient.GetAsync(new Uri(NetworkAddressResolver.Resolve(ApiMainUrl) + KeysPath));
            if (response.IsSuccessStatusCode)
                useBackupServer = false;
        }
        catch (Exception e)
        {
            Log.Error(e, "An error occured while validating the primary host");
        }

        var responseFromBackup = await httpClient.GetAsync(new Uri(NetworkAddressResolver.Resolve(BackupUrl) + KeysPath));
        if (responseFromBackup.IsSuccessStatusCode)
        {
            useBackupServer = true;
        }
    }

    public static string ApiUrl => useBackupServer  ? BackupUrl : ApiMainUrl;

#if DEBUG
    private const string ApiMainUrl = "https://localhost:7068";
#else
    private string ApiMainUrl = "https://ddapi.tomb.my.id";
#endif

    private const string BackupUrl = "https://wcsdance.azurewebsites.net";
    private const string KeysPath = "/.well-known/openid-configuration/jwks";

    private static void InitializeDanceApiClient()
    {
        var retryPipeline = CreateRetryPipeline();

        var socketHandler = CreateSocketHandler();

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

    private static ResiliencePipeline<HttpResponseMessage> CreateRetryPipeline()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions { BackoffType = DelayBackoffType.Exponential, MaxRetryAttempts = 3 })
            .Build();
    }

    private static SocketsHttpHandler CreateSocketHandler()
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