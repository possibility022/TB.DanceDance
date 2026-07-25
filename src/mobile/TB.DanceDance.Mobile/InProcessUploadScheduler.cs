using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.Library.Services.Network;

namespace TB.DanceDance.Mobile;

public sealed class InProcessUploadScheduler(IServiceProvider serviceProvider) : IUploadScheduler
{
    private readonly Lock sync = new();
    private CancellationTokenSource? cancellation;
    private Task? runningTask;

    public Task ScheduleAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync)
        {
            if (runningTask is { IsCompleted: false })
                return Task.CompletedTask;

            cancellation?.Dispose();
            cancellation = new CancellationTokenSource();
            runningTask = Task.Run(() => RunAsync(cancellation.Token), CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        Task? task;
        lock (sync)
        {
            cancellation?.Cancel();
            task = runningTask;
        }

        if (task is not null)
            await task.WaitAsync(cancellationToken);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<UploadWorker>();
            await worker.Work(progress: null, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "In-process upload worker failed.");
        }
    }
}
