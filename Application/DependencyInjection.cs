using Application.Services;
using Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
    {

        services
            .AddScoped<IVideoService, VideoService>()
            .AddScoped<IEventService, EventService>()
            .AddScoped<IGroupService, GroupService>()
            .AddScoped<IVideoUploaderService, VideoUploaderService>();

        return services;
    }
}