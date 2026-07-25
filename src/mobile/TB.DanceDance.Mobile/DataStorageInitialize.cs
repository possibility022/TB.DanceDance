using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models.Storage;

namespace TB.DanceDance.Mobile;

public sealed class DataStorageInitialize : IUploadStoreInitializer
{
    private readonly IDbContextFactory<VideosDbContext> dbContextFactory;
    private readonly Lazy<Task> initialization;

    public DataStorageInitialize(IDbContextFactory<VideosDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
        initialization = new Lazy<Task>(InitializeCoreAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) =>
        initialization.Value.WaitAsync(cancellationToken);

    private async Task InitializeCoreAsync()
    {
        Serilog.Log.Information("Initializing data storage started");
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await UploadQueueSchemaUpgrader.UpgradeAsync(dbContext);

        var completedJobs = await dbContext.VideosToUpload
            .Where(job => job.State == UploadJobState.Completed && job.OwnsFile)
            .ToArrayAsync();

        foreach (var job in completedJobs)
        {
            try
            {
                File.Delete(job.FullFileName);
                job.OwnsFile = false;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Could not clean completed upload file {Path}", job.FullFileName);
            }
        }

        await dbContext.SaveChangesAsync();
        Serilog.Log.Information("Initializing data storage complete");
    }
}