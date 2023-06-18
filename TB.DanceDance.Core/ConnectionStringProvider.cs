using Microsoft.Extensions.Configuration;

namespace TB.DanceDance.Core;

public static class ConnectionStringProvider
{

    /// <summary>
    /// Trying to provide connection string. Priority has configuration.
    /// Last on the list is ConnectionString from environments.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="connectionStringName"></param>
    /// <param name="appSettingsKey"></param>
    /// <param name="environmentSettingName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static string GetConnectionString(IConfiguration configuration, string connectionStringName, string appSettingsKey, string environmentSettingName)
    {
        var cs = configuration.GetConnectionString(connectionStringName);

        if (cs != null)
            return cs;

        var section = configuration.GetSection(appSettingsKey);
        if (section?.Value != null)
            return section.Value;

        cs = Environment.GetEnvironmentVariable(connectionStringName);
        if (cs != null)
            return cs;

        cs = Environment.GetEnvironmentVariable(environmentSettingName);

        if (string.IsNullOrEmpty(cs))
            throw new Exception("Could not resolve connection string.");

        return cs;
    }

    public static string GetBlobConnectionString(IConfiguration configuration)
    {
        return GetConnectionString(configuration,
            "CUSTOMCONNSTR_Blob",
            "ConnectionStrings:Blob",
            "TB.DanceDance.ConnectionString.Blob");
    }

    public static string GetPostgreSqlDbConnectionString(IConfiguration configuration)
    {
        return GetConnectionString(configuration,
            "POSTGRESQLCONNSTR_PostgreDb",
            "ConnectionStrings:PostgreDb",
            "TB.DanceDance.ConnectionString.PostgreDb");
    }

    public static string GetPostgreIdentityStoreDbConnectionString(IConfiguration configuration)
    {
        return GetConnectionString(configuration,
            "POSTGRESQLCONNSTR_PostgreDbIdentityStore",
            "ConnectionStrings:PostgreDbIdentityStore",
            "TB.DanceDance.ConnectionString.PostgreDbIdentityStore");
    }
}
