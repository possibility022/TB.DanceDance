namespace TB.Auth.Web;

public sealed class AuthServerOptions
{
    public const string SectionName = "AuthServer";

    public string Issuer { get; set; } = "https://localhost:7259/";
    public string[] AllowedCorsOrigins { get; set; } = [];
    public string? ServerSigningCertificateBase64 { get; set; }
    public string? ServerSigningCertificatePassword { get; set; }
    public string? ServerEncryptionCertificateBase64 { get; set; }
    public string? ServerEncryptionCertificatePassword { get; set; }
    public string? ClientSigningCertificateBase64 { get; set; }
    public string? ClientSigningCertificatePassword { get; set; }
    public string? ClientEncryptionCertificateBase64 { get; set; }
    public string? ClientEncryptionCertificatePassword { get; set; }
}
