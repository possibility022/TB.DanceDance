using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TB.DanceDance.Identity.Extensions;

public static class Extensions
{
    public static IIdentityServerBuilder RegisterIdenityServerStorage(this IIdentityServerBuilder builder, string connectionString)
    {
        var assembly = DesignTimeContextFactory.GetMigrationAssembly();

        return builder.AddConfigurationStore(options =>
        {
            options.ConfigureDbContext = b =>
            {
                b.UseNpgsql(connectionString, postgre => postgre.MigrationsAssembly(assembly));
            };

            options.DefaultSchema = DesignTimeContextFactory.ConfigurationDbContextDefaultSchema;
        })
        .AddOperationalStore(options =>
        {
            options.ConfigureDbContext = b =>
            {
                b.UseNpgsql(connectionString, postgre => postgre.MigrationsAssembly(assembly));
            };

            options.DefaultSchema = DesignTimeContextFactory.PersistedGrantDbContextDefaultSchema;
        });
    }
}
