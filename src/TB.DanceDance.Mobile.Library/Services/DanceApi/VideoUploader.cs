using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models.Storage;
using TB.DanceDance.Mobile.Library.Services.Network;

namespace TB.DanceDance.Mobile.Library.Services.DanceApi;

public class VideoUploader
{
    private readonly BlobUploader uploader;
    private readonly IDanceHttpApiClient apiClient;
    private readonly VideosDbContext dbContext;
    private readonly Channel<UploadProgressEvent> notificationChannel;

    private FileInfo? currentlyUploadedFile;

    public VideoUploader(IDanceHttpApiClient apiClient, VideosDbContext dbContext,
        Channel<UploadProgressEvent> notificationChannel, NetworkAddressResolver networkAddressResolver)
    {
        uploader = new BlobUploader(networkAddressResolver);
        uploader.UploadProgress += _uploaderOnUploadProgress;
        this.apiClient = apiClient;
        this.dbContext = dbContext;
        this.notificationChannel = notificationChannel;
    }

    ~VideoUploader()
    {
        uploader.UploadProgress -= _uploaderOnUploadProgress;
    }

    private void _uploaderOnUploadProgress(object? sender, int e)
    {
        if (currentlyUploadedFile is not null)
        {
            notificationChannel.Writer.WriteAsync(new UploadProgressEvent()
            {
                FileName = currentlyUploadedFile.Name, FileSize = currentlyUploadedFile.Length, SendBytes = e
            });
        }
    }

    public async Task Upload(VideosToUpload videoToUpload, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(videoToUpload);

        if (videoToUpload.Uploaded)
            return;

        if (videoToUpload.Sas == null)
            throw new Exception("Sas is null"); //todo

        if (videoToUpload.SasExpireAt < DateTime.Now.AddMinutes(-5))
            throw new Exception("Sas expired"); //todo

        currentlyUploadedFile = new FileInfo(videoToUpload.FullFileName);
        await using var fileStream = currentlyUploadedFile.OpenRead();
        await uploader.UploadAsync(fileStream, new Uri(videoToUpload.Sas), token);

        videoToUpload.Uploaded = true;
    }

    public async Task AddToUploadList(string? name, string filePath, Guid groupId, CancellationToken token)
    {
        var fileInfo = new FileInfo(filePath);
        if (string.IsNullOrWhiteSpace(name))
            name = fileInfo.Name;

        var existingEntry =
            await dbContext.VideosToUpload.FirstOrDefaultAsync(r => r.FullFileName == filePath,
                cancellationToken: token);

        if (existingEntry?.Uploaded == true)
            return;

        var uploadInformation = await apiClient.GetUploadInformation(fileInfo.Name,
            name,
            SharingWithType.Group,
            groupId,
            fileInfo.CreationTimeUtc
        );

        if (uploadInformation == null)
            throw new Exception("Upload Information could not be found");

        dbContext.VideosToUpload.Add(MapToEntity(fileInfo, uploadInformation));
        await dbContext.SaveChangesAsync(token);
        StartUploading();
    }

    private void StartUploading()
    {
#if ANDROID
        UploadForegroundService.StartService();
#endif
    }

    private static VideosToUpload MapToEntity(FileInfo fileInfo, UploadVideoInformationResponse uploadInformation)
    {
        return new VideosToUpload()
        {
            Id = Guid.NewGuid(),
            FileName = fileInfo.Name,
            Uploaded = false,
            FullFileName = fileInfo.FullName,
            Sas = uploadInformation.Sas,
            RemoteVideoId = uploadInformation.VideoId,
            SasExpireAt = uploadInformation.ExpireAt.UtcDateTime,
        };
    }

    private async Task AddToUploadList(string filePath, Guid groupOrEventId, SharingWithType sharingWith, CancellationToken token)
    {
        FileInfo fileInfo = new FileInfo(filePath);

        var existingEntry =
            await dbContext.VideosToUpload.FirstOrDefaultAsync(r => r.FullFileName == filePath,
                cancellationToken: token);

        if (existingEntry?.Uploaded == true)
            return;

        var uploadInformation = await apiClient.GetUploadInformation(fileInfo.Name,
            fileInfo.Name,
            sharingWith,
            groupOrEventId,
            fileInfo.CreationTimeUtc
        );

        if (uploadInformation == null)
            throw new Exception("Upload Information could not be found");

        dbContext.VideosToUpload.Add(MapToEntity(fileInfo, uploadInformation));
        await dbContext.SaveChangesAsync(token);
        StartUploading();
    }

    public Task UploadVideoToGroup(string filePath, Guid groupId, CancellationToken token)
        => AddToUploadList(filePath, groupId, SharingWithType.Group, token);

    public Task UploadVideoToEvent(string filePath, Guid eventId, CancellationToken token)
        => AddToUploadList(filePath, eventId, SharingWithType.Event, token);
}