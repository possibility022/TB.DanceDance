using FFMpegCore;
using Serilog;
using TB.DanceDance.Services.Converter.Deamon.OAuthClient;

namespace TB.DanceDance.Services.Converter.Deamon;
internal class ProgramConfig
{
    public static ProgramConfig Instance { get; private set; } = new ProgramConfig();

    public string FFMPGDefaultFolder { get; internal set; }

    public string ApiOrigin { get; internal set; } = string.Empty;
    public string OAuthOrigin { get; internal set; } = string.Empty;

    public string WorkDir { get; internal set; } = "mediafolder";

    public TokenProviderOptions TokenProviderOptions { get; private set; } = new TokenProviderOptions { ClientId = "", ClientSecret = "", Scope = "" };

    public int DelayInMinutes { get; internal set; } = 5;

    private static bool TryGetEnvironmentVariable(string key, out string value)
    {
        var v = Environment.GetEnvironmentVariable($"tb.dancedance.converter.{key}");
        value = v ?? string.Empty;
        return !string.IsNullOrEmpty(v);
    }

    public static void Configure()
    {
        var config = new ProgramConfig();

        ConfigureLogging();
        ConfigureFfmpeg();
        ConfigureApi(config);
        ConfigureAuth(config);
        ConfigureMediaFolder(config);
        ConfigureDelay(config);

        ProgramConfig.Instance = config;
    }

    private static void ConfigureDelay(ProgramConfig config)
    {
        var executionHourSet = TryGetEnvironmentVariable("delayInMinutes", out var value);
        if (executionHourSet)
        {
            var parsed = int.TryParse(value, out var hour);
            if (parsed)
            {
                config.DelayInMinutes = hour;
            }
            else
            {
                Log.Warning("Execution hour could not be parsed. Given value: {0}", value);
            }
        }
    }

    private static void ConfigureMediaFolder(ProgramConfig config)
    {
        var workdirSet = TryGetEnvironmentVariable("workdir", out var workdir);

        if (workdirSet)
        {
            config.WorkDir = workdir;
        }
        Log.Information("Workdir set to: {0}", config.WorkDir);
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
            Log.Error("Scope is not set");

        if (string.IsNullOrEmpty(config.TokenProviderOptions.ClientId))
            Log.Error("ClientId is not set");

        if (string.IsNullOrEmpty(config.TokenProviderOptions.ClientSecret))
            Log.Error("ClientSecret is not set");

        if (string.IsNullOrEmpty(config.OAuthOrigin))
            Log.Error("OAuthOrigin is not set");
    }

    private static void ConfigureApi(ProgramConfig config)
    {
        var apiSet = TryGetEnvironmentVariable("apiOrigin", out var api);
        if (apiSet)
            config.ApiOrigin = api;

        if (File.Exists("api.set.txt"))
        {
            var lines = File.ReadAllLines("api.set.txt");
            if (!string.IsNullOrEmpty(lines[0]?.Trim()))
                config.ApiOrigin = lines[0].Trim();
        }

        if (string.IsNullOrEmpty(config.ApiOrigin))
            Log.Error("Api Origin is not set");
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

        if (!string.IsNullOrEmpty(path))
        {
            GlobalFFOptions.Configure(new FFOptions()
            {
                BinaryFolder = path,
            });
        }
    }

    private static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File($"danceDanceConverter.log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}
