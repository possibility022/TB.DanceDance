using Microsoft.Extensions.DependencyInjection;

namespace TB.DanceDance.Utilities.Mediating;

public sealed class MediatorBuilder
{
    private readonly IServiceCollection services;
    private readonly MediatorHandlerRegistry registry;

    internal MediatorBuilder(IServiceCollection services, MediatorHandlerRegistry registry)
    {
        this.services = services;
        this.registry = registry;
    }

    public MediatorBuilder Register<TRequest, TResponse, THandler>()
        where TRequest : IRequest<TResponse>
        where THandler : class, IRequestHandler<TRequest, TResponse>
    {
        services.AddScoped<IRequestHandler<TRequest, TResponse>, THandler>();
        registry.AddHandler<TRequest, TResponse>();
        return this;
    }

    public MediatorBuilder RegisterNotificationHandler<TNotification, THandler>()
        where TNotification : INotification
        where THandler : class, INotificationHandler<TNotification>
    {
        services.AddScoped<INotificationHandler<TNotification>, THandler>();
        return this;
    }

    public MediatorBuilder AddBehavior(Type openBehaviorType)
    {
        services.AddScoped(typeof(IPipelineBehavior<,>), openBehaviorType);
        return this;
    }
}
