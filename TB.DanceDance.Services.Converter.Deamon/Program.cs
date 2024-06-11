using TB.DanceDance.Services.Converter.Deamon;
using TB.DanceDance.Services.Converter.Deamon.OAuthClient;

ProgramConfig.Configure();

using var oauthClient = new HttpClient()
{
    BaseAddress = new Uri(ProgramConfig.Instance.OAuthOrigin)
};

var tokenProvider = new TokenProvider(oauthClient, ProgramConfig.Instance.TokenProviderOptions);

var handler = new TokenHttpHandler(tokenProvider);

using var apiHttpClient = new HttpClient(handler)
{
    BaseAddress = new Uri(ProgramConfig.Instance.ApiOrigin)
};

using var defaultHttpClient = new HttpClient();

var client = new DanceDanceApiClient(apiHttpClient, defaultHttpClient);


CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
CancellationToken token = cancellationTokenSource.Token;

var deamon = new Deamon(client);
await deamon.WorkAsync(token);

