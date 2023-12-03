using FFMpegCore;
using Serilog;
using TB.DanceDance.Services.Converter.Deamon.OAuthClient;

namespace TB.DanceDance.Services.Converter.Deamon;
internal class ProgramConfig
{
    public static class Settings
    {
        public const string FFMPGDefaultFolder = "D:\\Programy\\ffmpeg-2022-12-04-git-6c814093d8-full_build\\bin";

        public static string ApiOrigin { get; internal set; } = string.Empty;
        public static string OAuthOrigin { get; internal set; } = string.Empty;
    }

    public static TokenProviderOptions TokenProviderOptions { get; private set; } = null!;


    public static void Configure()
    {
        ConfigureLogging();
        ConfigureFfmpeg();
        ConfigureApi();
        ConfigureAuth();
    }

    private static void ConfigureAuth()
    {
        var lines = File.ReadAllLines("auth.set.txt");
        TokenProviderOptions = new TokenProviderOptions()
        {
            ClientId = lines[0].Trim(),
            ClientSecret = lines[1].Trim(),
            Scope = lines[2].Trim()
        };

        Settings.OAuthOrigin = lines[3];
    }

    private static void ConfigureApi()
    {
        var lines = File.ReadAllLines("api.set.txt");
        Settings.ApiOrigin = lines[0].Trim();
    }

    private static void ConfigureFfmpeg()
    {
        string path = Settings.FFMPGDefaultFolder;

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
