using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

public class AuthStoreContext : DbContext
{
    public const string DefaultSchema = "Idp.Auth";

    public AuthStoreContext(DbContextOptions<AuthStoreContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(DefaultSchema);
        builder.UseOpenIddict();
        base.OnModelCreating(builder);
    }
}