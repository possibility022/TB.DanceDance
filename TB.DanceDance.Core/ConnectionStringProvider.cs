using Microsoft.Extensions.Configuration;

namespace TB.DanceDance.Core
{
    public static class ConnectionStringProvider
    {
        private static string GetConnectionString(IConfiguration configuration, string connectionStringName, string appSettingsKey, string environmentSettingName)
        {
            var cs = configuration.GetConnectionString(connectionStringName);

            if (cs != null)
                return cs;

            var section = configuration.GetSection(appSettingsKey);
            if (section?.Value != null)
                return section.Value;

            cs = Environment.GetEnvironmentVariable(environmentSettingName);

            if (string.IsNullOrEmpty(cs))
                throw new Exception("Could not resolve connection string.");

            return cs;
        }

        public static string GetMongoDbConnectionString(IConfiguration configuration)
        {
            return GetConnectionString(configuration,
                "MongoDB",
                "ConnectionStrings:MongoDB",
                "TB.DanceDance.ConnectionString.Mongo");
        }

        public static string GetBlobConnectionString(IConfiguration configuration)
        {
            return GetConnectionString(configuration,
                "Blob",
                "ConnectionStrings:Blob",
                "TB.DanceDance.ConnectionString.Blob");
        }

        public static string GetMongoDbConnectionStringForIdentityStore(IConfiguration configuration)
        {
            return GetConnectionString(configuration,
                "MongoDBIdentityStore",
                "ConnectionStrings:MongoDBIdentityStore",
                "TB.DanceDance.ConnectionString.MongoDBIdentityStore");
        }

        public static string GetPostgreSqlDbConnectionString(IConfiguration configuration)
        {
            return GetConnectionString(configuration,
                "PostgreDb",
                "ConnectionStrings:PostgreDb",
                "TB.DanceDance.ConnectionString.PostgreDb");
        }

        public static string GetPostgreIdentityStoreDbConnectionString(IConfiguration configuration)
        {
            return GetConnectionString(configuration,
                "PostgreDbIdentityStore",
                "ConnectionStrings:PostgreDbIdentityStore",
                "TB.DanceDance.ConnectionString.PostgreDbIdentityStore");
        }
    }
}
