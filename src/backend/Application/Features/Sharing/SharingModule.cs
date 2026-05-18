using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.Sharing;

public static class SharingModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSharingFeature()
        {
            services.AddScoped<ISharedLinkService, SharedLinkService>();
            return services;
        }
    }
}
