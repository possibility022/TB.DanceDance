using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Identity;

public abstract class DesignTimeContextFactory :
    IDesignTimeDbContextFactory<IdentityStoreContext>

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
}
