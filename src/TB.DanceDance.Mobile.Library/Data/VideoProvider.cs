using TB.DanceDance.Mobile.Library.Data.Models;
using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.Library.Data;

public class VideoProvider
{
    private readonly IDanceHttpApiClient apiClient;

    public VideoProvider(IDanceHttpApiClient apiClient)
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

    public async Task<IReadOnlyCollection<Video>> GetMyVideos()
    {
        var response = await apiClient.GetMyVideos();
        var videos = Video.MapFromApiResponse(response);
        return videos;
    }
}