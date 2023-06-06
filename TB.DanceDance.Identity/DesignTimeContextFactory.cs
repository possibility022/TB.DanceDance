using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace TB.DanceDance.Identity
{
    public class DesignTimeContextFactory : IDesignTimeDbContextFactory<IdentityStoreContext>
    {
        public IdentityStoreContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<IdentityStoreContext>();
            optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Userid=postgres;Password=rgFraWIuyxONqWCQ71wh;Database=identitystore");

            return new IdentityStoreContext(optionsBuilder.Options);
        }
    }
}
