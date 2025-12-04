using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Identity;

public class DesignTimeContextFactory :
    IDesignTimeDbContextFactory<IdentityStoreContext>,
    IDesignTimeDbContextFactory<PersistedGrantDbContext>,
    IDesignTimeDbContextFactory<ConfigurationDbContext>

{

    public static string GetMigrationAssembly()
    {
#pragma warning disable CS8603 // Possible null reference return.
        return typeof(DesignTimeContextFactory).Assembly.GetName().Name;
#pragma warning restore CS8603 // Possible null reference return.
    }

    public const string ConfigurationDbContextDefaultSchema = "IdpServer.Config";
    public const string PersistedGrantDbContextDefaultSchema = "IdpServer.Oper";

    IdentityStoreContext IDesignTimeDbContextFactory<IdentityStoreContext>.CreateDbContext(string[] args)
    {
        return CreateIdentityStoreContext();
    }

    public static IdentityStoreContext CreateIdentityStoreContext(string? connectionString = null, Action<DbContextOptionsBuilder<IdentityStoreContext>>? options = null)
    {
        if (connectionString == null)
            connectionString =
                "Server=localhost;Port=5432;Userid=postgres;Password=rgFraWIuyxONqWCQ71wh;Database=identitystore";
        
        var optionsBuilder = new DbContextOptionsBuilder<IdentityStoreContext>();
        optionsBuilder.UseNpgsql(connectionString);
        
        options?.Invoke(optionsBuilder);

        return new IdentityStoreContext(optionsBuilder.Options);
    }

    ConfigurationDbContext IDesignTimeDbContextFactory<ConfigurationDbContext>.CreateDbContext(string[] args)
    {
        return CreateConfigurationDbContext();
    }

    public static ConfigurationDbContext CreateConfigurationDbContext(string? connectionString = null,  Action<DbContextOptionsBuilder<ConfigurationDbContext>>? options = null)
    {
        if (connectionString == null)
            connectionString =
                "Server=localhost;Port=5432;Userid=postgres;Password=rgFraWIuyxONqWCQ71wh;Database=identitystore";
        
        var assemblyName = GetMigrationAssembly();

        var optionsBuilder = new DbContextOptionsBuilder<ConfigurationDbContext>();

        optionsBuilder.UseNpgsql(connectionString,
            b => b.MigrationsAssembly(assemblyName)
        ); ;
        
        options?.Invoke(optionsBuilder);

        return new ConfigurationDbContext(optionsBuilder.Options, new IdentityServer4.EntityFramework.Options.ConfigurationStoreOptions()
        {
            DefaultSchema = ConfigurationDbContextDefaultSchema
        });
    }

    PersistedGrantDbContext IDesignTimeDbContextFactory<PersistedGrantDbContext>.CreateDbContext(string[] args)
    {
        return CreatePersistedGrantDbContext();
    }

    public static PersistedGrantDbContext CreatePersistedGrantDbContext(string? connectionString = null,  Action<DbContextOptionsBuilder<PersistedGrantDbContext>>? options = null)
    {
        if (connectionString == null)
            connectionString =
                "Server=localhost;Port=5432;Userid=postgres;Password=rgFraWIuyxONqWCQ71wh;Database=identitystore";
        
        var assemblyName = GetMigrationAssembly();

        var optionsBuilder = new DbContextOptionsBuilder<PersistedGrantDbContext>();
        optionsBuilder.UseNpgsql(connectionString,
            b => b.MigrationsAssembly(assemblyName));

        options?.Invoke(optionsBuilder);
        
        return new PersistedGrantDbContext(optionsBuilder.Options, new IdentityServer4.EntityFramework.Options.OperationalStoreOptions
        {
            DefaultSchema = PersistedGrantDbContextDefaultSchema
        });
    }
}
