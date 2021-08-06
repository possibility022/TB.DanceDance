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

            var databaseConnectionString = File.ReadAllText("databaseConnectionString.txt");

            //var loader = new Loader(DanceType.WestCoastSwing,
            //    databaseConnectionString,
            //    File.ReadAllText("blobConnectionString.txt"));
            
            //var task = loader.LoadData(@"C:\Users\TomaszBak\Downloads\West coast swing");
            //task.Wait();

            var rename = new VideoRename(databaseConnectionString);
            await rename.Rename();


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
