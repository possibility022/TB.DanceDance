using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TB.DanceDance.Data.Db
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            var connectionString = TryGetConnectionStringFromEnvironmentVariables();
            if (connectionString == null)
                throw new Exception("Could not get connection string from environments.");  //todo change this.

            optionsBuilder.UseSqlServer(connectionString);
            
            return new ApplicationDbContext(optionsBuilder.Options);
        }

        public static string? TryGetConnectionStringFromEnvironmentVariables()
        {
            var connectionString = Environment.GetEnvironmentVariable("TB.DanceDance.ConnectionString");
            return connectionString;
        }
    }
}
