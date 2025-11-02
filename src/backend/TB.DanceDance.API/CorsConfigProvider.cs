namespace TB.DanceDance.API
{
    public static class CorsConfigProvider
    {

        record CorsConfigSection
        {
            public const string POSITION = "TB:DanceDance:Cors";
            public string[] AllowedOrigins { get; set; } = [];
        }
        
        public static string[] GetDevOrigins()
        {
            return ["http://localhost:3000/", "http://localhost:3000", "https://localhost:3000/", "https://localhost:3000"
            ];
        }

        public static string[] GetFromEnvironmentVariable(IConfiguration configuration)
        {
            var config = new CorsConfigSection();

            configuration.GetSection(CorsConfigSection.POSITION)
                .Bind(config);

            return config.AllowedOrigins;
        }
    }
}
