using IdentityModel;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using System.Diagnostics;
using System.Text;

namespace TB.DanceDance.Mobile.Pages;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }

    // This method must be in a class in a platform project, even if
    // the HttpClient object is constructed in a shared project.
    public HttpClientHandler GetInsecureHandler()
    {
        HttpClientHandler handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            if (cert.Issuer.Equals("CN=localhost"))
                return true;
            return errors == System.Net.Security.SslPolicyErrors.None;
        };
        return handler;
    }
    
    private async void Button_OnClicked(object? sender, EventArgs e)
    {
        try
        {
            var client = new OidcClient(new OidcClientOptions()
            {
                //Authority = "https://localhost:7068",
                Authority = "https://10.0.2.2:7068",
                ClientId = "tbdancedancefront",
                RedirectUri = "myapp://",
                Scope = "openid tbdancedanceapi.read offline_access profile",
                Browser = new MauiAuthenticationBrowser(),
                BackchannelHandler = GetInsecureHandler(),
                Policy = new Policy()
                {
                    Discovery = new DiscoveryPolicy()
                    {
                        RequireHttps = false
                    }
                }
                
            });

            var result = await client.LoginAsync();

            var currentAccessToken = result.AccessToken;

            Debug.WriteLine(currentAccessToken);

            var sb = new StringBuilder(128);

            sb.AppendLine("claims:");
            foreach (var claim in result.User.Claims)
            {
                sb.AppendLine($"{claim.Type}: {claim.Value}");
            }

            sb.AppendLine();
            sb.AppendLine("access token:");
            sb.AppendLine(result.AccessToken);

            if (!string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                sb.AppendLine();
                sb.AppendLine("refresh token:");
                sb.AppendLine(result.RefreshToken);
            }

            Debug.Write(sb.ToString());
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());    
        }
    }
}