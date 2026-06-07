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

    public async Task<(IReadOnlyCollection<Video> Items, int TotalCount)> GetEventVideos(Guid eventId, int page, int pageSize)
    {
        var response = await apiClient.GetVideosForEvent(eventId, page, pageSize);
        var videos = Video.MapFromApiResponse(response.Items.ToArray());
        return (videos, response.TotalCount);
    }

    public async Task<(IReadOnlyCollection<Video> Items, int TotalCount)> GetGroupVideosAsync(int page, int pageSize)
    {
        var response = await apiClient.GetVideosFromGroups(page, pageSize);
        var videos = Video.MapFromApiResponse(response.Items.ToArray());
        return (videos, response.TotalCount);
    }

    public async Task<(IReadOnlyCollection<Video> Items, int TotalCount)> GetMyVideos(int page, int pageSize)
    {
        var response = await apiClient.GetMyVideos(page, pageSize);
        var videos = Video.MapFromApiResponse(response.Items.ToArray());
        return (videos, response.TotalCount);
    }
}