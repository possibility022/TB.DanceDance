namespace TB.DanceDance.Services.Converter.Deamon.OAuthClient;

class TokenProviderOptions
{
    public required string Scope { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
}
