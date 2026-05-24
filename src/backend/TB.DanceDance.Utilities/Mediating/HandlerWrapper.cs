using Microsoft.Extensions.DependencyInjection;

namespace TB.DanceDance.Utilities.Mediating;

internal interface IHandlerWrapper<TResponse>
{
    Task<TResponse> HandleAsync(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal sealed class HandlerWrapper<TRequest, TResponse> : IHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> HandleAsync(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .ToArray();

        RequestHandlerDelegate<TResponse> pipeline = () => handler.HandleAsync(typedRequest, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            var b = behavior;
            pipeline = () => b.HandleAsync(typedRequest, next, cancellationToken);
        }

        return pipeline();
    }
}
