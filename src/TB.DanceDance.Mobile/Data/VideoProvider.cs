using TB.DanceDance.Mobile.Models;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.Data;

public class VideoProvider
{
    private readonly DanceHttpApiClient apiClient;

    public VideoProvider(DanceHttpApiClient apiClient)
    {
        this.apiClient = apiClient;
    }
    
    public async Task<List<Video>> GetEventVideos(Guid eventId)
    {
        var videosForEvent = await apiClient.GetVideosForEvent(eventId);
        var videos = Video.MapFromApiResponse(videosForEvent);
        return videos;
    }
    
    public async Task<List<Video>> GetGroupVideosAsync()
    {
        var response = await apiClient.GetVideosFromGroups();
        var videos = Video.MapFromApiResponse(response);
        return videos;
    }
}