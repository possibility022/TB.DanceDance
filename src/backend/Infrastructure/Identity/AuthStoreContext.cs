using Microsoft.EntityFrameworkCore;

namespace TB.Auth.Web;

public class AuthStoreContext : DbContext
{
    public const string DefaultSchema = "Idp.Auth";

    public AuthStoreContext(DbContextOptions<AuthStoreContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(DefaultSchema);
        base.OnModelCreating(builder);
    }
}
