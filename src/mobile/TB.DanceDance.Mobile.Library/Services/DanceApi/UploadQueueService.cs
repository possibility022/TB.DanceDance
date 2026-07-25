using Microsoft.EntityFrameworkCore;
using TB.DanceDance.API.Contracts.Features.Videos;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models.Storage;

namespace TB.DanceDance.Mobile.Library.Services.DanceApi;

public sealed record UploadQueueRequest(
    string SourcePath,
    SharingWithType SharingWithType,
    Guid? SharedWithId = null,
    string? VideoName = null);

public sealed record UploadQueueResult(string SourcePath, Guid? JobId, string? Error)
{
    public bool Succeeded => JobId.HasValue;
}

public sealed class UploadQueueOptions
{
    public required string StagingDirectory { get; init; }
}

public interface IUploadScheduler
{
    Task ScheduleAsync(CancellationToken cancellationToken = default);
    Task CancelAsync(CancellationToken cancellationToken = default);
}

public interface IUploadQueueService
{
    Task<IReadOnlyList<UploadQueueResult>> EnqueueAsync(
        IEnumerable<UploadQueueRequest> requests,
        CancellationToken cancellationToken = default);

    Task RetryAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid jobId, CancellationToken cancellationToken = default);
}

public sealed class UploadQueueService(
    IDbContextFactory<VideosDbContext> dbContextFactory,
    IUploadScheduler scheduler,
    UploadQueueOptions options) : IUploadQueueService
{
    public async Task<IReadOnlyList<UploadQueueResult>> EnqueueAsync(
        IEnumerable<UploadQueueRequest> requests,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(options.StagingDirectory);
        var results = new List<UploadQueueResult>();
        var stagedJobs = new List<VideosToUpload>();

        foreach (var request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? stagedPath = null;
            try
            {
                var source = new FileInfo(request.SourcePath);
                if (!source.Exists)
                    throw new FileNotFoundException("The selected video is no longer available.", request.SourcePath);

                var jobId = Guid.NewGuid();
                var extension = Path.GetExtension(source.Name);
                stagedPath = Path.Combine(options.StagingDirectory, $"{jobId:N}{extension}");

                await using (var sourceStream = source.OpenRead())
                await using (var targetStream = new FileStream(
                                 stagedPath,
                                 FileMode.CreateNew,
                                 FileAccess.Write,
                                 FileShare.None,
                                 1024 * 1024,
                                 FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await sourceStream.CopyToAsync(targetStream, cancellationToken);
                }

                var now = DateTime.UtcNow;
                stagedJobs.Add(new VideosToUpload
                {
                    Id = jobId,
                    FullFileName = stagedPath,
                    FileName = source.Name,
                    VideoName = string.IsNullOrWhiteSpace(request.VideoName) ? source.Name : request.VideoName,
                    SharingWithType = request.SharingWithType,
                    SharedWithId = request.SharedWithId,
                    RecordedTimeUtc = source.CreationTimeUtc,
                    FileSize = source.Length,
                    State = UploadJobState.PendingRegistration,
                    OwnsFile = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
                results.Add(new UploadQueueResult(request.SourcePath, jobId, null));
            }
            catch (OperationCanceledException)
            {
                if (stagedPath is not null)
                    TryDelete(stagedPath);
                throw;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (stagedPath is not null)
                    TryDelete(stagedPath);
                results.Add(new UploadQueueResult(request.SourcePath, null, ex.Message));
            }
        }

        if (stagedJobs.Count == 0)
            return results;

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            dbContext.VideosToUpload.AddRange(stagedJobs);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var job in stagedJobs)
                TryDelete(job.FullFileName);
            throw;
        }

        await scheduler.ScheduleAsync(cancellationToken);
        return results;
    }

    public async Task RetryAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await dbContext.VideosToUpload.SingleAsync(x => x.Id == jobId, cancellationToken);
        if (job.State is UploadJobState.Completed or UploadJobState.Cancelled)
            return;

        job.State = job.RemoteVideoId == Guid.Empty
            ? UploadJobState.PendingRegistration
            : UploadJobState.PendingUpload;
        job.AttemptCount = 0;
        job.NextAttemptAtUtc = null;
        job.LastError = null;
        job.CancellationRequested = false;
        job.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await scheduler.ScheduleAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await dbContext.VideosToUpload.SingleAsync(x => x.Id == jobId, cancellationToken);
        if (job.State == UploadJobState.Completed)
            return;

        job.CancellationRequested = true;
        job.State = UploadJobState.Cancelled;
        job.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await scheduler.CancelAsync(cancellationToken);
        if (job.OwnsFile)
            TryDelete(job.FullFileName);
        await scheduler.ScheduleAsync(cancellationToken);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Could not delete staged upload file {Path}", path);
        }
    }
}
