using Microsoft.Extensions.DependencyInjection;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Tests.TestsFixture;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Tests;

/// <summary>
/// Base for module integration tests. Provides dedicated seed contexts (one per module) for arranging
/// data, and a <see cref="Send{TResponse}"/> helper that dispatches a request through the real module
/// wiring — the same way the API resolves <c>IRequestHandler&lt;,&gt;</c> from DI. Handlers run in
/// their own scope (fresh DbContexts) and read the data committed via the seed contexts.
/// </summary>
public abstract class BaseTestClass : IAsyncLifetime
{
    private readonly DanceDbFixture dbFixture;

    protected AccessDbContext SeedAccessContext { get; private set; } = null!;
    protected VideosDbContext SeedVideosContext { get; private set; } = null!;
    protected ServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Blob storage connection used by the wired blob factory. Tests that exercise blob-backed
    /// handlers override this with their <c>BlobStorageFixture</c> connection string.
    /// </summary>
    protected virtual string BlobConnectionString => "UseDevelopmentStorage=true";

    protected BaseTestClass(DanceDbFixture dbFixture)
    {
        this.dbFixture = dbFixture;
    }

    public async ValueTask InitializeAsync()
    {
        SeedAccessContext = dbFixture.CreateAccessDbContext();
        SeedVideosContext = dbFixture.CreateVideosDbContext();
        Services = dbFixture.BuildServiceProvider(BlobConnectionString);
        await Initialize();
    }

    protected virtual ValueTask Initialize() => ValueTask.CompletedTask;

    protected virtual ValueTask BeforeDispose() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await BeforeDispose();
        await SeedAccessContext.DisposeAsync();
        await SeedVideosContext.DisposeAsync();
        await Services.DisposeAsync();
    }

    /// <summary>
    /// Dispatches <paramref name="request"/> through the wired modules and returns the handler's
    /// result. <typeparamref name="TResponse"/> is inferred from the request's
    /// <see cref="IRequest{TResponse}"/> marker.
    /// </summary>
    protected async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        await using var scope = Services.CreateAsyncScope();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = scope.ServiceProvider.GetRequiredService(handlerType);

        // Invoke through the public IRequestHandler<,> interface — the concrete handler classes are
        // internal to their modules, so a `dynamic` call (which binds against the runtime type) cannot
        // see HandleAsync from the test assembly.
        var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.HandleAsync))!;
        try
        {
            return await (Task<TResponse>)method.Invoke(handler, [request, ct])!;
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Surface the handler's real exception (e.g. ArgumentException) rather than the reflection wrapper.
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // unreachable
        }
    }
}
