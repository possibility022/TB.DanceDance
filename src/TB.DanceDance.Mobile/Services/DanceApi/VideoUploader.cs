using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Data.Models.Storage;
using TB.DanceDance.Mobile.Services.Network;

namespace TB.DanceDance.Mobile.Services.DanceApi;

public class VideoUploader
{
    private readonly BlobUploader uploader;
    private readonly DanceHttpApiClient apiClient;
    private readonly VideosDbContext dbContext;
    private readonly Channel<UploadProgressEvent> notificationChannel;

    private FileInfo? currentlyUploadedFile;
    
    public VideoUploader(DanceHttpApiClient apiClient, VideosDbContext dbContext, Channel<UploadProgressEvent> notificationChannel)
    {
        uploader = new BlobUploader();
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
        await uploader.ResumeUploadAsync(fileStream, new Uri(videoToUpload.Sas), token);

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

    public async Task UploadVideoToEvent(string filePath, Guid eventId, CancellationToken token)
    {
        FileInfo fileInfo = new FileInfo(filePath);

        var existingEntry =
            await dbContext.VideosToUpload.FirstOrDefaultAsync(r => r.FullFileName == filePath,
                cancellationToken: token);

        if (existingEntry?.Uploaded == true)
            return;

        var uploadInformation = await apiClient.GetUploadInformation(fileInfo.Name,
            fileInfo.Name,
            SharingWithType.Event,
            eventId,
            fileInfo.CreationTimeUtc
        );

        if (uploadInformation == null)
            throw new Exception("Upload Information could not be found");

        dbContext.VideosToUpload.Add(MapToEntity(fileInfo, uploadInformation));
        await dbContext.SaveChangesAsync(token);
    }
}