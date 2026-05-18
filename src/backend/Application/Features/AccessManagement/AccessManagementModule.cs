using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.AccessManagement;

public static class AccessManagementModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAccessManagementFeature()
        {
            services.AddScoped<IAccessService, AccessService>();
            services.AddScoped<IAccessManagementService, AccessManagementService>();
            return services;
        }
    }
}
