using TB.DanceDance.Mobile.Models;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.Data;

public class VideoProvider
{
    private readonly DanceHttpApiClient apiClient;
    private readonly VideosDbContext videoDbContext;

    public VideoProvider(DanceHttpApiClient apiClient, VideosDbContext videoDbContext)
    {
        this.apiClient = apiClient;
        this.videoDbContext = videoDbContext;
    }

    public async Task<List<Video>> GetEventVideos(Guid eventId)
    {
        var videosForEvent = await apiClient.GetVideosForEvent(eventId);
        var videos = Video.MapFromApiResponse(videosForEvent);
        SetUploadState(videos);

        return videos;
    }

    private void SetUploadState(List<Video> videos)
    {
        var videosToUpload = videoDbContext.VideosToUpload
            .Where(r => r.Uploaded == false)
            .ToDictionary(r => r.RemoteVideoId);

        foreach (var video in videos)
        {
            if (videosToUpload.TryGetValue(video.Id, out var videoToUpload))
            {
                video.UploadState = new UploadState() { Uploaded = videoToUpload.Uploaded, };
            }
        }
    }

    public async Task<List<Video>> GetGroupVideosAsync()
    {
        var response = await apiClient.GetVideosFromGroups();
        var videos = Video.MapFromApiResponse(response);
        SetUploadState(videos);
        return videos;
    }
}