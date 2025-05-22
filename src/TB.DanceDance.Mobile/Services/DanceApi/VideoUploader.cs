using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Data.Models.Storage;

namespace TB.DanceDance.Mobile.Services.DanceApi;

public class VideoUploader
{
    private readonly BlobUploader _uploader;
    private readonly DanceHttpApiClient _apiClient;
    private readonly VideosDbContext _dbContext;

    public VideoUploader(BlobUploader uploader, DanceHttpApiClient apiClient, VideosDbContext dbContext)
    {
        _uploader = uploader;
        _apiClient = apiClient;
        _dbContext = dbContext;
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

        FileInfo fileInfo = new FileInfo(videoToUpload.FullFileName);
        await using var fileStream = fileInfo.OpenRead();
        await _uploader.ResumeUploadAsync(fileStream, new Uri(videoToUpload.Sas), token);

        videoToUpload.Uploaded = true;
    }

    public async Task AddToUploadList(string? name, string filePath, Guid groupId, CancellationToken token)
    {
        var fileInfo = new FileInfo(filePath);
        if (string.IsNullOrWhiteSpace(name))
            name = fileInfo.Name;

        var existingEntry =
            await _dbContext.VideosToUpload.FirstOrDefaultAsync(r => r.FullFileName == filePath,
                cancellationToken: token);

        if (existingEntry?.Uploaded == true)
            return;

        var uploadInformation = await _apiClient.GetUploadInformation(fileInfo.Name,
            name,
            SharingWithType.Group,
            groupId,
            fileInfo.CreationTimeUtc
        );

        if (uploadInformation == null)
            throw new Exception("Upload Information could not be found");

        _dbContext.VideosToUpload.Add(MapToEntity(fileInfo, uploadInformation));
        await _dbContext.SaveChangesAsync(token);
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
            await _dbContext.VideosToUpload.FirstOrDefaultAsync(r => r.FullFileName == filePath,
                cancellationToken: token);

        if (existingEntry?.Uploaded == true)
            return;

        var uploadInformation = await _apiClient.GetUploadInformation(fileInfo.Name,
            fileInfo.Name,
            SharingWithType.Event,
            eventId,
            fileInfo.CreationTimeUtc
        );

        if (uploadInformation == null)
            throw new Exception("Upload Information could not be found");

        _dbContext.VideosToUpload.Add(MapToEntity(fileInfo, uploadInformation));
        await _dbContext.SaveChangesAsync(token);
    }
}