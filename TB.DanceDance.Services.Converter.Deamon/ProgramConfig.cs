using FFMpegCore;
using Serilog;

namespace TB.DanceDance.Services.Converter.Deamon;
internal class ProgramConfig
{

    public static class Settings
    {
        public const string FFMPGPath = "D:\\Programy\\ffmpeg-2022-12-04-git-6c814093d8-full_build\\bin\\ffmpeg.exe";
        public const string FFMPGFolder = "D:\\Programy\\ffmpeg-2022-12-04-git-6c814093d8-full_build\\bin";
        public const string Args = "i [INPUT_FILE_PATH] -c:v libvpx-vp9 -b:v 2M [OUTPUT_FILE_PATH]";
    }


    public static void Configure()
    {
        ConfigureLogging();
        ConfigureFfmpeg();
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
            .WriteTo.File($"log-{DateTime.UtcNow.ToString("s").Replace(":", "")}.txt", rollingInterval: RollingInterval.Infinite)
            .CreateLogger();
    }
}
