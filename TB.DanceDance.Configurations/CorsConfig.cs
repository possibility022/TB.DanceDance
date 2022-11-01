using System;

namespace TB.DanceDance.Configurations
{
    public class CorsConfig
    {
        public string[] AllowedOrigins { get; set; } = new string[0];

        public static string[] GetDevOrigins()
        {
            return new[] { "http://localhost:3000/", "http://localhost:3000", "https://localhost:3000/", "https://localhost:3000" };
        }

        public static CorsConfig GetFromEnvironmentVariable()
        {
            var origins = Environment.GetEnvironmentVariable("TB.DanceDance.Cors.Origins");
            var config = new CorsConfig();
            if (!string.IsNullOrEmpty(origins))
            {
                config.AllowedOrigins = origins.Split(";");
            }

            return config;
        }
    }
}
