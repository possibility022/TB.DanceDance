using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TB.Auth.Web.Identity;

public class DesignTimeContextFactory :
    IDesignTimeDbContextFactory<IdentityStoreContext>,
    IDesignTimeDbContextFactory<AuthStoreContext>
{
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
    
    AuthStoreContext IDesignTimeDbContextFactory<AuthStoreContext>.CreateDbContext(string[] args)
    {
        return CreateAuthStoreContext();
    }
    
    public static string GetMigrationAssembly()
    {
        return typeof(DesignTimeContextFactory).Assembly.GetName().Name!;
    }
    
    public static AuthStoreContext CreateAuthStoreContext(string? connectionString = null, Action<DbContextOptionsBuilder<AuthStoreContext>>? options = null)
    {
        if (connectionString == null)
            connectionString =
                "Server=localhost;Port=5432;Userid=postgres;Password=rgFraWIuyxONqWCQ71wh;Database=authstore";
        
        var assemblyName = GetMigrationAssembly();

        var optionsBuilder = new DbContextOptionsBuilder<AuthStoreContext>();
        optionsBuilder.UseNpgsql(connectionString,
            b => b.MigrationsAssembly(assemblyName));

        options?.Invoke(optionsBuilder);
        
        return new AuthStoreContext(optionsBuilder.Options);
    }
}
