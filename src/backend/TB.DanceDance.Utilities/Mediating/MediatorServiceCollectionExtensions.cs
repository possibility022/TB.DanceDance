using Microsoft.Extensions.DependencyInjection;

namespace TB.DanceDance.Utilities.Mediating;

public static class MediatorServiceCollectionExtensions
{
    public static MediatorBuilder AddMediator(this IServiceCollection services)
    {
        var registry = new MediatorHandlerRegistry();
        services.AddSingleton(registry);
        services.AddScoped<IMediator, Mediator>();
        return new MediatorBuilder(services, registry);
    }
}
