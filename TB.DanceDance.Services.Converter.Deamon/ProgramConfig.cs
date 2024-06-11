using FFMpegCore;
using Serilog;
using TB.DanceDance.Services.Converter.Deamon.OAuthClient;

namespace TB.DanceDance.Services.Converter.Deamon;
internal class ProgramConfig
{
    public static ProgramConfig Instance { get; private set; } = new ProgramConfig();

    public string FFMPGDefaultFolder { get; internal set; } = "D:\\Programy\\ffmpeg-2022-12-04-git-6c814093d8-full_build\\bin";

    public string ApiOrigin { get; internal set; } = string.Empty;
    public string OAuthOrigin { get; internal set; } = string.Empty;

    public TokenProviderOptions TokenProviderOptions { get; private set; } = null!;

    private static bool TryGetEnvironmentVariable(string key, out string value)
    {
        value = string.Empty;
        var v = Environment.GetEnvironmentVariable($"tb.dancedance.converter.{key}");
        return !string.IsNullOrEmpty(v);
    }

    public static void Configure()
    {
        var config = new ProgramConfig();

        ConfigureLogging();
        ConfigureFfmpeg();
        ConfigureApi(config);
        ConfigureAuth(config);

        ProgramConfig.Instance = config;
    }

    private static void ConfigureAuth(ProgramConfig config)
    {
        var scopeSet = TryGetEnvironmentVariable("scope", out var scope);
        var clientIdSet = TryGetEnvironmentVariable("clientId", out var clientId);
        var clientSecretSet = TryGetEnvironmentVariable("clientSecret", out var clientSecret);
        var oAuthOriginSet = TryGetEnvironmentVariable("oAuthOrigin", out var oAuthOrigin);

        if (scopeSet)
            config.TokenProviderOptions.Scope = scope;

        if (clientIdSet)
            config.TokenProviderOptions.ClientId = clientId;

        if (clientSecretSet)
            config.TokenProviderOptions.ClientSecret = clientSecret;

        if (oAuthOriginSet)
            config.OAuthOrigin = oAuthOrigin;

        if (File.Exists("auth.set.txt"))
        {
            var lines = File.ReadAllLines("auth.set.txt");
            config.TokenProviderOptions = new TokenProviderOptions()
            {
                ClientId = lines[0].Trim(),
                ClientSecret = lines[1].Trim(),
                Scope = lines[2].Trim()
            };
            config.OAuthOrigin = lines[3];

        }
        if (string.IsNullOrEmpty(config.TokenProviderOptions.Scope))
            Log.Warning("Scope is not set");

        if (string.IsNullOrEmpty(config.TokenProviderOptions.ClientId))
            Log.Warning("ClientId is not set");

        if (string.IsNullOrEmpty(config.TokenProviderOptions.ClientSecret))
            Log.Warning("ClientSecret is not set");

        if (string.IsNullOrEmpty(config.OAuthOrigin))
            Log.Warning("OAuthOrigin is not set");
    }

    private static void ConfigureApi(ProgramConfig config)
    {
        var apiSet = TryGetEnvironmentVariable("apiOrigin", out var api);
        if (apiSet)
            config.ApiOrigin = api;

        var lines = File.ReadAllLines("api.set.txt");
        if (!string.IsNullOrEmpty(lines[0]?.Trim()))
            config.ApiOrigin = lines[0].Trim();

        if (string.IsNullOrEmpty(config.ApiOrigin))
            Log.Warning("Api Origin is not set");
    }

    private static void ConfigureFfmpeg()
    {
        string path = Instance.FFMPGDefaultFolder;

        if (File.Exists("ffmpgpath.txt"))
        {
            var lines = File.ReadAllLines("ffmpgpath.txt");
            if (!string.IsNullOrEmpty(lines[0]))
            {
                path = lines[0];
            }
        }

        GlobalFFOptions.Configure(new FFOptions()
        {
            BinaryFolder = path,
        });
    }

    private static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File($"danceDanceConverter.log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}
