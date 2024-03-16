using Duende.IdentityServer.EntityFramework.DbContexts;
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
        var optionsBuilder = new DbContextOptionsBuilder<IdentityStoreContext>();
        optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Userid=postgres;Password=rgFraWIuyxONqWCQ71wh;Database=identitystore");

        return new IdentityStoreContext(optionsBuilder.Options);
    }

    ConfigurationDbContext IDesignTimeDbContextFactory<ConfigurationDbContext>.CreateDbContext(string[] args)
    {
        var assemblyName = GetMigrationAssembly();

        var optionsBuilder = new DbContextOptionsBuilder<ConfigurationDbContext>();

        optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Userid=postgres;Password=rgFraWIuyxONqWCQ71wh;Database=identitystore",
            b => b.MigrationsAssembly(assemblyName)
            ); ;


        var context = new ConfigurationDbContext(optionsBuilder.Options);
        context.StoreOptions.DefaultSchema = ConfigurationDbContextDefaultSchema;
        return context;
    }

    PersistedGrantDbContext IDesignTimeDbContextFactory<PersistedGrantDbContext>.CreateDbContext(string[] args)
    {
        var assemblyName = GetMigrationAssembly();

        var optionsBuilder = new DbContextOptionsBuilder<PersistedGrantDbContext>();
        optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Userid=postgres;Password=rgFraWIuyxONqWCQ71wh;Database=identitystore",
            b => b.MigrationsAssembly(assemblyName));

        var context = new PersistedGrantDbContext(optionsBuilder.Options);
        context.StoreOptions.DefaultSchema = PersistedGrantDbContextDefaultSchema;
        return context;
    }
}
