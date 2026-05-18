using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.Groups;

public static class GroupsModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddGroupsFeature()
        {
            services.AddScoped<IGroupService, GroupService>();
            return services;
        }
    }
}
