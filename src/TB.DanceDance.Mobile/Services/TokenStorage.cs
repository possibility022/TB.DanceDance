using IdentityModel.OidcClient;

namespace TB.DanceDance.Mobile.Services;

static class TokenStorage
{
    public static LoginResult? LoginResult { get; set; }
}