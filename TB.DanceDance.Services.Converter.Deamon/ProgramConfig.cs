using FFMpegCore;
using Serilog;
using TB.DanceDance.Services.Converter.Deamon.OAuthClient;

namespace TB.DanceDance.Services.Converter.Deamon;
internal class ProgramConfig
{
    public static class Settings
    {
        public const string FFMPGPath = "D:\\Programy\\ffmpeg-2022-12-04-git-6c814093d8-full_build\\bin\\ffmpeg.exe";
        public const string FFProbePath = "D:\\Programy\\ffmpeg-2022-12-04-git-6c814093d8-full_build\\bin\\ffprobe.exe";
        public const string FFMPGFolder = "D:\\Programy\\ffmpeg-2022-12-04-git-6c814093d8-full_build\\bin";
    }

    public static TokenProviderOptions TokenProviderOptions { get; private set; } = null!;


    public static void Configure()
    {
        ConfigureLogging();
        ConfigureFfmpeg();
        ConfigureAuth();
    }

    private static void ConfigureAuth()
    {
        var lines = File.ReadAllLines("auth.settings");
        TokenProviderOptions = new TokenProviderOptions()
        {
            ClientId = lines[0].Trim(),
            ClientSecret = lines[1].Trim(),
            Scope = lines[2].Trim()
        };
    }

    private static void ConfigureFfmpeg()
    {
        GlobalFFOptions.Configure(new FFOptions()
        {
            BinaryFolder = Settings.FFMPGFolder,
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
