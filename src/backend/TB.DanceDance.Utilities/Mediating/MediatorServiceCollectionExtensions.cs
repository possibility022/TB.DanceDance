using Microsoft.Extensions.DependencyInjection;

namespace TB.DanceDance.Utilities.Mediating;

public static class MediatorServiceCollectionExtensions
{
    public static MediatorBuilder AddMediator(this IServiceCollection services)
    {
        var registry = new MediatorHandlerRegistry();
        services.AddSingleton(registry);
        return new MediatorBuilder(services, registry);
    }
}
