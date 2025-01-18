using IdentityModel.OidcClient;

namespace TB.DanceDance.Mobile.Services.Auth;

static class TokenStorage
{
    public static LoginResult? LoginResult { get; set; }
}