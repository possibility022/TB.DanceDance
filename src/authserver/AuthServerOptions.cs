namespace TB.Auth.Web;

public sealed class AuthServerOptions
{
    public const string SectionName = "AuthServer";

    public string Issuer { get; set; } = "https://localhost:7259/";
    public string[] AllowedCorsOrigins { get; set; } = [];
}
