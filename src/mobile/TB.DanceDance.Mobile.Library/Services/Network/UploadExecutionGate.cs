namespace TB.DanceDance.Mobile.Library.Services.Network;

public sealed class UploadExecutionGate
{
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public async Task<IDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        return new Releaser(semaphore);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private bool released;

        public void Dispose()
        {
            if (released)
                return;

            released = true;
            semaphore.Release();
        }
    }
}
