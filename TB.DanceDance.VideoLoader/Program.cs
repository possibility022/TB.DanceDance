using System;
using System.IO;
using Serilog;
using TB.DanceDance.Data.Models;

namespace TB.DanceDance.VideoLoader
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigureLogging();


            var loader = new Loader(DanceType.WestCoastSwing, 
                File.ReadAllText("databaseConnectionString.txt"),
                File.ReadAllText("blobConnectionString.txt"));
            
            var task = loader.LoadData(@"C:\Users\TomaszBak\Downloads\West coast swing");
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
