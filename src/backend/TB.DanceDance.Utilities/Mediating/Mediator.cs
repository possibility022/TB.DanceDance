using Microsoft.Extensions.DependencyInjection;

namespace TB.DanceDance.Utilities.Mediating;

public sealed class Mediator : IMediator
{
    private readonly IServiceProvider serviceProvider;
    private readonly MediatorHandlerRegistry registry;

    internal Mediator(IServiceProvider serviceProvider, MediatorHandlerRegistry registry)
    {
        this.serviceProvider = serviceProvider;
        this.registry = registry;
    }

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return registry.GetWrapper<TResponse>(request.GetType())
                        .HandleAsync(request, serviceProvider, cancellationToken);
    }

    public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        foreach (var handler in serviceProvider.GetServices<INotificationHandler<TNotification>>())
            await handler.HandleAsync(notification, cancellationToken);
    }
}
