using Azure;
using System.Threading.Channels;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Data.Models.Storage;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.Services.Network;

public class UploadWorker : IDisposable
{
    private readonly VideosDbContext dbContext;
    private readonly VideoUploader videoUploader;
    private readonly DanceHttpApiClient apiClient;
    private readonly Channel<UploadProgressEvent> uploadProgressChannel;
    private IPlatformNotification? platformNotification;
    private CancellationTokenSource? mainLoopCanncellationTokenSource;
    private CancellationTokenSource? currentVideoProcessCancellationSource;
    
    private bool isPaused = false;
    private readonly SemaphoreSlim pauseLock = new SemaphoreSlim(0, 1);
    private TimeSpan delay = TimeSpan.Zero;
    
    public UploadWorker(VideosDbContext dbContext, 
        VideoUploader videoUploader,
        DanceHttpApiClient apiClient,
        Channel<UploadProgressEvent> uploadProgressChannel)
    {
        this.dbContext = dbContext;
        this.videoUploader = videoUploader;
        this.apiClient = apiClient;
        this.uploadProgressChannel = uploadProgressChannel;
        Connectivity.ConnectivityChanged += ConnectivityOnConnectivityChanged;
    }

    public void SetPlatformNotification(IPlatformNotification notification)
    {
        this.platformNotification = notification;
    }

    private void ConnectivityOnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet && e.ConnectionProfiles.Contains(ConnectionProfile.WiFi))
        {
            Resume();
        }
        else
        {
            Paused();
        }
    }

    public async Task Work(CancellationToken token)
    {
        try
        {
            mainLoopCanncellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            Serilog.Log.Information("Starting looking for videos to upload.");

            var monitorProgressProcess = MonitorProgress(mainLoopCanncellationTokenSource!.Token);

            while (mainLoopCanncellationTokenSource!.IsCancellationRequested == false)
            {
                var videos = dbContext.VideosToUpload
                    .Where(r => r.Uploaded == false)
                    .ToArray();
                
                if (videos.Length == 0)
                    break;

                foreach (var vid in videos)
                {
                    await DelayIfRequired();
                    currentVideoProcessCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(mainLoopCanncellationTokenSource.Token);
                    
                    await Upload(vid, currentVideoProcessCancellationSource.Token);
                    
                    currentVideoProcessCancellationSource.Dispose();
                    currentVideoProcessCancellationSource = null;
                }
            }

            await mainLoopCanncellationTokenSource.CancelAsync();
            // for some reason, when we wait for monitorProgressProcess, it stucks...
            // await monitorProgressProcess;
            
            platformNotification?.UploadCompleteNotification();

            Serilog.Log.Information("All videos uploaded.");
        }
        catch (Exception ex)
        {
            Serilog.Log.Information(ex, "Foreground Service Exception.");
        }
    }

    private async Task MonitorProgress(CancellationToken cancellationToken)
    {
        while (await uploadProgressChannel.Reader.WaitToReadAsync(cancellationToken))
        {
            var message = await uploadProgressChannel.Reader.ReadAsync(cancellationToken);
            platformNotification?.UploadProgressNotification(message.FileName, message.SendBytes, message.FileSize);
        }
    }
    
    private async Task Upload(VideosToUpload video, CancellationToken token)
    {
        try
        {
            delay = TimeSpan.Zero;
            Serilog.Log.Information("Uploading one video.");
            if (video.SasExpireAt < DateTime.Now.AddMinutes(-6))
                await RefreshSas(video);
            
            await videoUploader.Upload(video, token);
            video.Uploaded = true;
            Serilog.Log.Information("Video uploaded.");

            // ReSharper disable once MethodSupportsCancellation
            await dbContext.SaveChangesAsync();
        }
        catch (TaskCanceledException taskCanceledException)
        {
            // Nothing to do, wait for resume
        }
        catch (RequestFailedException requestFailedException)
        {
            if (requestFailedException.Status == 403)
            {
                await RefreshSas(video);
            }
        }
        catch (Exception ex)
        {
            delay = TimeSpan.FromMinutes(1);
            Serilog.Log.Warning(ex, "Foreground Service Exception.");
        }
    }

    private async Task RefreshSas(VideosToUpload videoToUpload)
    {
        var newUrl = await apiClient.RefreshUploadUrl(videoToUpload.RemoteVideoId);
        videoToUpload.Sas = newUrl.Sas;
        videoToUpload.SasExpireAt = newUrl.ExpireAt.DateTime;
    }
    
    private async Task DelayIfRequired()
    {
        try
        {
            if (isPaused)
            {
                await pauseLock.WaitAsync();
            }
            else
            {
                await Task.Delay(delay, mainLoopCanncellationTokenSource!.Token);
            }
        }
        catch (TaskCanceledException exception)
        {
            // nothing to do here
        }
    }
    
    private void Paused()
    {
        isPaused = true;
        currentVideoProcessCancellationSource?.Cancel();
        platformNotification?.UploadPausedNotification();
    }

    private void Resume()
    {
        isPaused = false;
        if (pauseLock.CurrentCount < 1)
            pauseLock.Release();
    }

    private void ReleaseUnmanagedResources()
    {
        Connectivity.ConnectivityChanged -= ConnectivityOnConnectivityChanged;
        this.currentVideoProcessCancellationSource?.Dispose();
        this.mainLoopCanncellationTokenSource?.Dispose();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~UploadWorker()
    {
        ReleaseUnmanagedResources();
    }
}