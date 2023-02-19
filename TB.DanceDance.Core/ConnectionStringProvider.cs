using Microsoft.Extensions.Configuration;

namespace TB.DanceDance.Core
{
    public static class ConnectionStringProvider
    {
        public static string GetMongoDbConnectionString(IConfiguration configuration)
        {
            var section = configuration.GetRequiredSection("ConnectionStrings:MongoDB");

            var cs = section.Value;

            if (cs == null)
                cs = Environment.GetEnvironmentVariable("TB.DanceDance.ConnectionString.Mongo");


            if (string.IsNullOrEmpty(cs))
                throw new Exception("Could not resolve connection string for mongo db.");

            return cs;
        }

        public static string GetBlobConnectionString(IConfiguration configuration)
        {
            // I know I know, it is a copy-paste...

            var section = configuration.GetRequiredSection("ConnectionStrings:Blob");

            var cs = section.Value;

            if (cs == null)
                cs = Environment.GetEnvironmentVariable("TB.DanceDance.ConnectionString.Blob");


            if (string.IsNullOrEmpty(cs))
                throw new Exception("Could not resolve connection string for mongo db.");

            return cs;
        }
    }
}
