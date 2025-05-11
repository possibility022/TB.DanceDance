using System.Diagnostics;
using System.Net.Http.Json;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.Mobile.Services.Auth;

namespace TB.DanceDance.Mobile.Services.DanceApi;

public class DanceHttpApiClient
{
    private readonly HttpClient httpClient;

    public DanceHttpApiClient(IHttpClientFactory httpClientFactory)
    {
        this.httpClient = httpClientFactory.CreateClient(nameof(DanceHttpApiClient));
    }

    public async Task<UserEventsAndGroupsResponse?> GetUserAccesses()
    {
        var response = await httpClient.GetAsync("/api/videos/accesses/my");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<UserEventsAndGroupsResponse>();
        return content;
    }

    public async Task<ICollection<GroupWithVideosResponse>?> GetVideosFromGroups()
    {
        var response = await httpClient.GetAsync("/api/groups/videos");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadFromJsonAsync<ICollection<GroupWithVideosResponse>>();

        return content;
    }

    public async Task<ICollection<VideoInformationResponse>> GetVideosForEvent(Guid eventId)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/events/{eventId}/videos");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<ICollection<VideoInformationResponse>>();
            if (content == null)
                return Array.Empty<VideoInformationResponse>();

            return content;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            throw;
        }
    }

    public async Task<UploadVideoInformationResponse?> GetUploadInformation(
        string fileName,
        string nameOfVideo,
        SharingWithType sharingWith,
        Guid sharedWithId,
        DateTime recordedTimeUtc
        )
    {
        SharedVideoInformationRequest request = new()
        {
            SharingWithType = sharingWith,
            FileName = fileName,
            SharedWith = sharedWithId,
            NameOfVideo = nameOfVideo,
            RecordedTimeUtc = recordedTimeUtc
        };
        
        var response = await httpClient.PostAsJsonAsync("/api/videos/upload", request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<UploadVideoInformationResponse>();
        return content;
    }

    public async Task<Stream> GetStream(string videoBlobId)
    {
        var responseMessage = await httpClient.GetAsync($"/api/videos/{videoBlobId}/stream", HttpCompletionOption.ResponseHeadersRead);
        responseMessage.EnsureSuccessStatusCode();

        return await responseMessage.Content.ReadAsStreamAsync();
    }
    
    public Uri GetVideoUri(string videoBlobId)
    {
        var builder = new UriBuilder(httpClient.BaseAddress);
        builder.Path = $"/api/videos/{videoBlobId}/stream";
        builder.Query = $"?token={TokenStorage.LoginResult?.AccessToken}";

        return builder.Uri;
    }
}