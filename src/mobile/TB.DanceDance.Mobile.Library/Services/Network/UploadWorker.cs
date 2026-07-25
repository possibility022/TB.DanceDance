using Azure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Diagnostics;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models.Storage;
using TB.DanceDance.Mobile.Library.Services.Auth;
using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.Library.Services.Network;

public enum UploadRunResult
{
    Complete,
    Retry
}

public sealed class UploadWorker(
    IDbContextFactory<VideosDbContext> dbContextFactory,
    IVideoUploader videoUploader,
    IDanceHttpApiClient apiClient,
    [FromKeyedServices(TokenStorage.PrimaryStorageKey)] ITokenProviderService tokenProvider,
    UploadExecutionGate executionGate,
    IUploadStoreInitializer storeInitializer,
    IUploadQueueChangeNotifier changeNotifier)
{
    private const int MaxAttempts = 8;
    private static readonly TimeSpan ProgressPersistenceInterval = TimeSpan.FromSeconds(2);

    public async Task<UploadRunResult> Work(
        IProgress<UploadProgressEvent>? progress,
        CancellationToken cancellationToken)
    {
        await storeInitializer.EnsureInitializedAsync(cancellationToken);
        using var execution = await executionGate.EnterAsync(cancellationToken);
        using var authentication = BackgroundAuthenticationContext.RequireSilentAuthentication();

        if (await tokenProvider.GetAccessTokenSilently() is null)
        {
            await MarkRunnableJobsWaitingForAuthentication(cancellationToken);
            return UploadRunResult.Retry;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var now = DateTime.UtcNow;
            var job = await dbContext.VideosToUpload
                .Where(x => !x.CancellationRequested)
                .Where(x => x.State == UploadJobState.PendingRegistration
                            || x.State == UploadJobState.PendingUpload
                            || x.State == UploadJobState.Uploading
                            || x.State == UploadJobState.RetryScheduled
                            || x.State == UploadJobState.WaitingForAuthentication)
                .Where(x => x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now)
                .OrderBy(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (job is null)
                return await HasDeferredJobs(dbContext, cancellationToken)
                    ? UploadRunResult.Retry
                    : UploadRunResult.Complete;

            await ProcessJob(dbContext, job, progress, cancellationToken);
            changeNotifier.NotifyChanged();
            if (job.State == UploadJobState.WaitingForAuthentication)
                return UploadRunResult.Retry;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return UploadRunResult.Retry;
    }

    private async Task ProcessJob(
        VideosDbContext dbContext,
        VideosToUpload job,
        IProgress<UploadProgressEvent>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            if (job.FileSize <= 0 && File.Exists(job.FullFileName))
                job.FileSize = new FileInfo(job.FullFileName).Length;

            job.State = job.RemoteVideoId == Guid.Empty
                ? UploadJobState.PendingRegistration
                : UploadJobState.Uploading;
            job.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            if (job.RemoteVideoId == Guid.Empty)
                await RegisterUpload(dbContext, job, cancellationToken);

            if (job.SasExpireAt <= DateTime.UtcNow.AddMinutes(5))
                await RefreshSas(dbContext, job, cancellationToken);

            job.State = UploadJobState.Uploading;
            job.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            var lastPersistenceTimestamp = Stopwatch.GetTimestamp();
            var progressPersistence = Task.CompletedTask;
            var byteProgress = new InlineProgress<long>(bytes =>
            {
                job.UploadedBytes = bytes;
                if (progressPersistence.IsCompleted
                    && Stopwatch.GetElapsedTime(lastPersistenceTimestamp) >= ProgressPersistenceInterval)
                {
                    lastPersistenceTimestamp = Stopwatch.GetTimestamp();
                    progressPersistence = PersistProgressAsync(job.Id, bytes);
                }
                progress?.Report(new UploadProgressEvent
                {
                    FileName = job.FileName,
                    FileSize = job.FileSize,
                    SendBytes = bytes
                });
            });

            try
            {
                await videoUploader.Upload(job, byteProgress, cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 403)
            {
                await RefreshSas(dbContext, job, cancellationToken);
                await videoUploader.Upload(job, byteProgress, cancellationToken);
            }

            await progressPersistence;
            job.Uploaded = true;
            job.UploadedBytes = job.FileSize;
            job.State = UploadJobState.Completed;
            job.LastError = null;
            job.NextAttemptAtUtc = null;
            job.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            if (DeleteOwnedFile(job))
            {
                job.OwnsFile = false;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            changeNotifier.NotifyChanged();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await dbContext.Entry(job).ReloadAsync(CancellationToken.None);
            if (!job.CancellationRequested && job.State != UploadJobState.Cancelled)
            {
                job.State = job.RemoteVideoId == Guid.Empty
                    ? UploadJobState.PendingRegistration
                    : UploadJobState.PendingUpload;
                job.UpdatedAtUtc = DateTime.UtcNow;
                await SaveWithoutCancellation(dbContext);
            }
            throw;
        }
        catch (Exception ex)
        {
            await RecordFailure(dbContext, job, ex, cancellationToken);
        }
    }

    private async Task RegisterUpload(
        VideosDbContext dbContext,
        VideosToUpload job,
        CancellationToken cancellationToken)
    {
        var uploadInformation = await apiClient.GetUploadInformation(
            job.FileName,
            string.IsNullOrWhiteSpace(job.VideoName) ? job.FileName : job.VideoName,
            job.SharingWithType,
            job.SharedWithId,
            job.RecordedTimeUtc,
            cancellationToken);

        if (uploadInformation is null)
            throw new HttpRequestException("The API did not return upload information.");

        job.RemoteVideoId = uploadInformation.VideoId;
        job.Sas = uploadInformation.Sas;
        job.SasExpireAt = uploadInformation.ExpireAt.UtcDateTime;
        job.State = UploadJobState.PendingUpload;
        job.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RefreshSas(
        VideosDbContext dbContext,
        VideosToUpload job,
        CancellationToken cancellationToken)
    {
        var refreshed = await apiClient.RefreshUploadUrl(job.RemoteVideoId, cancellationToken);
        job.Sas = refreshed.Sas;
        job.SasExpireAt = refreshed.ExpireAt.UtcDateTime;
        job.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task RecordFailure(
        VideosDbContext dbContext,
        VideosToUpload job,
        Exception exception,
        CancellationToken cancellationToken)
    {
        job.AttemptCount++;
        job.LastError = exception.Message;
        job.UpdatedAtUtc = DateTime.UtcNow;

        if (IsAuthenticationFailure(exception))
        {
            job.State = UploadJobState.WaitingForAuthentication;
            job.NextAttemptAtUtc = null;
        }
        else if (IsPermanentFailure(exception) || job.AttemptCount >= MaxAttempts)
        {
            job.State = UploadJobState.Failed;
            job.NextAttemptAtUtc = null;
        }
        else
        {
            job.State = UploadJobState.RetryScheduled;
            var delayMinutes = Math.Min(Math.Pow(2, job.AttemptCount - 1), 360);
            job.NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(delayMinutes);
        }

        Serilog.Log.Warning(exception, "Upload job {JobId} entered state {State}", job.Id, job.State);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkRunnableJobsWaitingForAuthentication(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var jobs = await dbContext.VideosToUpload
            .Where(x => x.State == UploadJobState.PendingRegistration
                        || x.State == UploadJobState.PendingUpload
                        || x.State == UploadJobState.Uploading
                        || x.State == UploadJobState.RetryScheduled)
            .ToListAsync(cancellationToken);

        foreach (var job in jobs)
        {
            job.State = UploadJobState.WaitingForAuthentication;
            job.LastError = "Sign in to continue uploading.";
            job.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Task<bool> HasDeferredJobs(
        VideosDbContext dbContext,
        CancellationToken cancellationToken) =>
        dbContext.VideosToUpload.AnyAsync(
            x => x.State == UploadJobState.RetryScheduled
                 || x.State == UploadJobState.WaitingForNetwork,
            cancellationToken);

    private static bool IsAuthenticationFailure(Exception exception) =>
        exception is BackgroundAuthenticationRequiredException
        || exception is HttpRequestException
        {
            StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
        };

    private static bool IsPermanentFailure(Exception exception) =>
        exception is FileNotFoundException
            or DirectoryNotFoundException
            or UnauthorizedAccessException
            or UriFormatException
        || exception is HttpRequestException
        {
            StatusCode: >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError
                and not HttpStatusCode.RequestTimeout
                and not HttpStatusCode.TooManyRequests
                and not HttpStatusCode.Unauthorized
                and not HttpStatusCode.Forbidden
        };

    private static async Task SaveWithoutCancellation(VideosDbContext dbContext)
    {
        try
        {
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Could not persist upload cancellation state.");
        }
    }

    private async Task PersistProgressAsync(Guid jobId, long uploadedBytes)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var persisted = await dbContext.VideosToUpload.FindAsync(jobId);
            if (persisted is null)
                return;

            persisted.UploadedBytes = uploadedBytes;
            persisted.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
            changeNotifier.NotifyChanged();
        }
        catch (Exception ex)
        {
            Serilog.Log.Debug(ex, "Could not persist progress for upload job {JobId}", jobId);
        }
    }

    private static bool DeleteOwnedFile(VideosToUpload job)
    {
        if (!job.OwnsFile)
            return false;

        try
        {
            File.Delete(job.FullFileName);
            return true;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Could not delete completed upload file {Path}", job.FullFileName);
            return false;
        }
    }
}
