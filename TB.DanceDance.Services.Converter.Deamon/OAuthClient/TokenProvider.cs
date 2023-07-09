using System.Net.Http.Json;
using System.Text;

namespace TB.DanceDance.Services.Converter.Deamon.OAuthClient;
internal class TokenProvider
{
    private readonly HttpClient oauthHttpClient;
    private readonly TokenProviderOptions options;

    private Token? currentToken = null;
    private DateTime expiresAt = DateTime.MinValue;

    public TokenProvider(HttpClient oauthHttpClient, TokenProviderOptions options)
    {
        this.oauthHttpClient = oauthHttpClient;
        this.options = options;
    }

    private HttpRequestMessage GetTokenRequest()
    {
        var body = new Dictionary<string, string>()
        {
            {"grant_type", "client_credentials" },
            {"scope", options.Scope }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(body)
        };

        var authHeader = EncodeUserAndPassword();
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

        return request;
    }

    private string EncodeUserAndPassword()
    {
        var value = $"{options.ClientId}:{options.ClientSecret}";
        var asBytes = Encoding.UTF8.GetBytes(value);
        var asBase64 = Convert.ToBase64String(asBytes);
        return asBase64;
    }

    public async Task<Token> GetTokenAsync(CancellationToken token)
    {
        if (currentToken != null && DateTime.Now > expiresAt)
            return currentToken;

        var request = GetTokenRequest();

        var res = await this.oauthHttpClient.SendAsync(request, token);
        res.EnsureSuccessStatusCode();

        var content = await res.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: token);

        if (content == null)
            throw new NullReferenceException("Expected content to be deserialized from response.");

        this.expiresAt = DateTime.Now.AddSeconds(content.ExpiresIn - 300);
        this.currentToken = new Token() { AccessToken = content.AccessToken, Schema = content.TokenType };

        return currentToken;
    }
}