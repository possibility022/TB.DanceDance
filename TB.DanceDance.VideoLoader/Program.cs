using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using TB.DanceDance.Data.Models;

namespace TB.DanceDance.VideoLoader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ConfigureLogging();

            var loader = new Loader(@"C:\Users\TomaszBak\Downloads\ffmpeg-master-latest-win64-gpl\bin\ffmpeg.exe");

            var task = loader.LoadData(@"G:\West\WebM2", "*.webm");
            task.Wait();


            Log.Information("Done");
        }

        private static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File($"log-{DateTime.UtcNow.ToString("s").Replace(":","")}.txt", rollingInterval: RollingInterval.Infinite)
                .CreateLogger();
        }
    }
}
