using Application.Services;
using Domain;
using Domain.Exceptions;
using Domain.Services;
using Infrastructure.Data;
using Infrastructure.Data.BlobStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterInfrastructureServices(IConfiguration configuration)
        {
            services.AddSingleton<IBlobDataServiceFactory>(r =>
                new BlobDataServiceFactory(configuration.GetConnectionString("Blob") ?? throw new AppException(
                    "Blob connection string is null.")));

            services.AddDbContext<DanceDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("PostgreDb") ?? throw new AppException(
                    "PostgreDb connection string is null."));
            });

            services.AddScoped<IAccessManagementService, AccessManagementService>();

            return services;
        }
    }
}
