using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.Events;

public static class EventsModule
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddEventsFeature()
        {
            services.AddScoped<IEventService, EventService>();
            return services;
        }
    }
}
