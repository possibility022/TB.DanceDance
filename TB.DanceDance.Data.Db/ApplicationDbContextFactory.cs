using System;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TB.DanceDance.Data.Db
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            ApplyOptions(optionsBuilder);

            return new ApplicationDbContext(optionsBuilder.Options);
        }

        public static void ApplyOptions(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = TryGetConnectionStringFromEnvironmentVariables();
            if (connectionString == null)
                throw new Exception("Could not get connection string from environments.");  //todo change this.

            //optionsBuilder.UseSqlServer(connectionString);
            optionsBuilder.UseCosmos(connectionString, "dancedanceapp", o =>
            {
                o.ConnectionMode(ConnectionMode.Direct);
            });
        }

        public static string? TryGetConnectionStringFromEnvironmentVariables()
        {
            var connectionString = Environment.GetEnvironmentVariable("TB.DanceDance.ConnectionString.Cosmos");
            return connectionString;
        }
    }
}
