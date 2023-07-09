namespace TB.DanceDance.Services.Converter.Deamon.OAuthClient;

class TokenProviderOptions
{
    public required string Scope { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}
