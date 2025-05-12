using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.Mobile.Data;

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
        if (videoToUpload == null)
            throw new ArgumentNullException(nameof(videoToUpload));

        if (videoToUpload.Uploaded)
            return;

        if (videoToUpload.Sas == null)
            throw new Exception("Sas is null"); //todo

        if (videoToUpload.SasExpireAt > DateTime.Now)
            throw new Exception("Sas expired"); //todo

        try
        {
            FileInfo fileInfo = new FileInfo(videoToUpload.FullFileName);
            await using var fileStream = fileInfo.OpenRead();
            await _uploader.UploadFileAsync(fileStream, new Uri(videoToUpload.Sas), token);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    public async Task AddToUploadList(string? name, string filePath, Guid groupId, CancellationToken token)
    {
        FileInfo fileInfo = new FileInfo(filePath);
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


        if (existingEntry is null)
        {
            _dbContext.VideosToUpload.Add(new VideosToUpload()
            {
                Id = Guid.NewGuid(),
                FileName = fileInfo.Name,
                Uploaded = false,
                FullFileName = fileInfo.FullName,
                Sas = uploadInformation.Sas,
                RemoteVideoId = uploadInformation.VideoId,
                SasExpireAt = uploadInformation.ExpireAt.UtcDateTime,
            });
            await _dbContext.SaveChangesAsync(token);
        }
    }

    public void UploadVideoToEvent(string filePath, Guid eventName)
    {
    }
}