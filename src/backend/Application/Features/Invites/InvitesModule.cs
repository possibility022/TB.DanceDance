using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.Invites;

public static class InvitesModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInvitesFeature()
        {
            services.AddScoped<IInviteLinkService, InviteLinkService>();
            return services;
        }
    }
}
