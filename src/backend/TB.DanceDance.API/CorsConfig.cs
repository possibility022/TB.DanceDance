namespace TB.DanceDance.API
{
    public class CorsConfig
    {
        public string[] AllowedOrigins { get; set; } = [];

        public static string[] GetDevOrigins()
        {
            return ["http://localhost:3000/", "http://localhost:3000", "https://localhost:3000/", "https://localhost:3000"
            ];
        }

        public static CorsConfig GetFromEnvironmentVariable(IConfiguration section)
        {
            var origins= section.GetSection("TB")
                .GetSection("DanceDance")
                .GetSection("Cors")["Origins"];
                
            var config = new CorsConfig();
            if (!string.IsNullOrEmpty(origins))
                config.AllowedOrigins = origins.Split(";");

            return config;
        }
    }
}
