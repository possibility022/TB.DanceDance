﻿using IdentityServer4.EntityFramework.DbContexts;
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

        return new ConfigurationDbContext(optionsBuilder.Options, new IdentityServer4.EntityFramework.Options.ConfigurationStoreOptions()
        {
            DefaultSchema = ConfigurationDbContextDefaultSchema
        });
    }

    PersistedGrantDbContext IDesignTimeDbContextFactory<PersistedGrantDbContext>.CreateDbContext(string[] args)
    {
        var assemblyName = GetMigrationAssembly();

        var optionsBuilder = new DbContextOptionsBuilder<PersistedGrantDbContext>();
        optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Userid=postgres;Password=rgFraWIuyxONqWCQ71wh;Database=identitystore",
            b => b.MigrationsAssembly(assemblyName));

        return new PersistedGrantDbContext(optionsBuilder.Options, new IdentityServer4.EntityFramework.Options.OperationalStoreOptions
        {
            DefaultSchema = PersistedGrantDbContextDefaultSchema
        });
    }
}
