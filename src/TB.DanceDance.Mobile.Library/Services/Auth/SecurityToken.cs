namespace TB.DanceDance.Mobile.Library.Services.Auth;

public record SecurityToken
{
    public required string? IdentityToken { get; set; }

    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    /// <value>
    /// The access token.
    /// </value>
    public required string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    /// <value>
    /// The refresh token.
    /// </value>
    public required string? RefreshToken { get; set; }

    public DateTimeOffset AccessTokenExpiration { get; set; }
};