namespace TB.DanceDance.Utilities.Mediating;

internal sealed class MediatorHandlerRegistry
{
    private readonly Dictionary<Type, object> wrappers = new();

    internal void AddHandler<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        wrappers[typeof(TRequest)] = new HandlerWrapper<TRequest, TResponse>();
    }

    internal IHandlerWrapper<TResponse> GetWrapper<TResponse>(Type requestType)
    {
        if (wrappers.TryGetValue(requestType, out var wrapper))
            return (IHandlerWrapper<TResponse>)wrapper;

        throw new InvalidOperationException(
            $"No handler registered for '{requestType.Name}'. " +
            $"Call builder.Register<{requestType.Name}, TResponse, THandler>() during mediator setup.");
    }
}
