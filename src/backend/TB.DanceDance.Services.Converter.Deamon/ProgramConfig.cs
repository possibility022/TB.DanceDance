using FFMpegCore;
using Serilog;
using TB.DanceDance.Services.Converter.Deamon.OAuthClient;

namespace TB.DanceDance.Services.Converter.Deamon;
internal class ProgramConfig
{
    public string FFMPGDefaultFolder { get; set; } = string.Empty;

    public string ApiOrigin { get; set; } = string.Empty;
    public string OAuthOrigin { get; set; } = string.Empty;

    public string WorkDir { get; set; } = "mediafolder";

    public TokenProviderOptions TokenProviderOptions { get; set; } = new TokenProviderOptions { ClientId = "", ClientSecret = "", Scope = "" };

    public int DelayInMinutes { get; set; } = 5;

    // When set, replaces the scheme/host/port of all blob SAS URLs before the converter
    // accesses them. Needed in Docker: the API generates SAS URLs with 127.0.0.1 (correct
    // for the browser on the host), but inside a container 127.0.0.1 is the container
    // itself. Set to the internal Docker service endpoint, e.g. http://azuriteStorage:10000.
    public string? BlobEndpointOverride { get; set; }

    public void ConfigureFfmpeg()
    {
        if (!string.IsNullOrEmpty(FFMPGDefaultFolder))
        {
            GlobalFFOptions.Configure(new FFOptions()
            {
                BinaryFolder = FFMPGDefaultFolder,
            });
        }
    }

    public void Validate()
    {
        if (string.IsNullOrEmpty(TokenProviderOptions.Scope))
            Log.Error("Scope is not set");

        if (string.IsNullOrEmpty(TokenProviderOptions.ClientId))
            Log.Error("ClientId is not set");

        if (string.IsNullOrEmpty(TokenProviderOptions.ClientSecret))
            Log.Error("ClientSecret is not set");

        if (string.IsNullOrEmpty(OAuthOrigin))
            Log.Error("OAuthOrigin is not set");

        if (string.IsNullOrEmpty(ApiOrigin))
            Log.Error("Api Origin is not set");

        Log.Information("Workdir set to: {0}", WorkDir);
    }
}
