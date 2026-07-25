using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models.Storage;

namespace TB.DanceDance.Mobile;

public class DataStorageInitialize : IMauiInitializeService
{
    private readonly VideosDbContext dbContext;

    public DataStorageInitialize(VideosDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public void Initialize(IServiceProvider services)
    {
        Serilog.Log.Information("Initializing data storage started");
        UploadQueueSchemaUpgrader.UpgradeAsync(dbContext).GetAwaiter().GetResult();
        CleanupCompletedStagedFiles();
        Serilog.Log.Information("Initializing data storage complete");
    }

    private void CleanupCompletedStagedFiles()
    {
        var completedJobs = dbContext.VideosToUpload
            .Where(job => job.State == UploadJobState.Completed && job.OwnsFile)
            .ToArray();

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

        dbContext.SaveChanges();
    }
}