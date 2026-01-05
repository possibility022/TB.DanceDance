using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.Mobile.Library.Services.Auth;

namespace TB.DanceDance.Mobile.Library.Services.DanceApi;

public class DanceHttpApiClient : IDanceHttpApiClient
{
    private readonly ITokenProviderService primaryTokenProviderService;
    private readonly ITokenProviderService secondaryTokenProviderService;
    private readonly HttpClient httpClient;

    public DanceHttpApiClient(IHttpClientFactory httpClientFactory,
        
        [FromKeyedServices(TokenStorage.PrimaryStorageKey)]ITokenProviderService primaryTokenProviderService,
        [FromKeyedServices(TokenStorage.SecondaryStorageKey)]ITokenProviderService secondaryTokenProviderService
        )
    {
        this.primaryTokenProviderService = primaryTokenProviderService;
        this.secondaryTokenProviderService = secondaryTokenProviderService;
        this.httpClient = httpClientFactory.CreateClient(nameof(DanceHttpApiClient));
    }

    public async Task RenameVideoAsync(Guid videoId, string newName)
    {
        var request = new VideoRenameRequest() { NewName = newName };
        var response = await httpClient.PostAsJsonAsync($"/api/videos/{videoId}/rename", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<UserEventsAndGroupsResponse> GetUserAccesses()
    {
        var response = await httpClient.GetAsync("/api/videos/accesses/my");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<UserEventsAndGroupsResponse>();
        return content ?? new UserEventsAndGroupsResponse();;
    }

    public async Task RequestAccess(RequestAssigmentModelRequest accessRequest)
    {
        var response = await httpClient.PostAsJsonAsync("/api/videos/accesses/request", accessRequest);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyCollection<GroupWithVideosResponse>?> GetVideosFromGroups()
    {
        var response = await httpClient.GetAsync("/api/groups/videos");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<GroupWithVideosResponse>>();

        return content;
    }

    public async Task<IReadOnlyCollection<VideoInformationResponse>> GetVideosForEvent(Guid eventId)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/events/{eventId}/videos");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<VideoInformationResponse>>();
            if (content == null)
                return Array.Empty<VideoInformationResponse>();

            return content;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error on getting videos for event {id}", eventId);
            throw;
        }
    }

    public async Task<UploadVideoInformationResponse> RefreshUploadUrl(Guid videoId)
    {
        var response = await httpClient.GetAsync($"/api/videos/upload/{videoId}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<UploadVideoInformationResponse>();
        return content!;
    }

    public async Task<UploadVideoInformationResponse?> GetUploadInformation(
        string fileName,
        string nameOfVideo,
        SharingWithType sharingWith,
        Guid? sharedWithId,
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
        var token = primaryTokenProviderService.GetValidAccessTokenNoFetch();
        if (token == null)
            token = secondaryTokenProviderService.GetValidAccessTokenNoFetch();
        
        var builder = new UriBuilder(httpClient.BaseAddress!)
        {
            Path = $"/api/videos/{videoBlobId}/stream", 
            Query = $"?token={token}"
        };

        return builder.Uri;
    }

    public async Task CreateEvent(string eventName, DateTime eventDate)
    {
        var body = new CreateNewEventRequest() { Event = new Event() { Date = eventDate, Name = eventName } };
        var res = await httpClient.PostAsJsonAsync($"/api/events", body);

        res.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyCollection<VideoInformationResponse>> GetMyVideos()
    {
        var response = await httpClient.GetAsync("/api/videos/my", CancellationToken.None);
        response.EnsureSuccessStatusCode();

        var videos = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<VideoInformationResponse>>();
        if (videos == null)
            videos = Array.Empty<VideoInformationResponse>();

        return videos;
    }

    public async Task<SharedLinkResponse?> GetSharingLinkAsync(Guid videoId, CancellationToken token = default)
    {
        CreateSharedLinkRequest request = new()
        {
            ExpirationDays = 7
        };

        var response = await httpClient.PostAsJsonAsync($"/api/videos/{videoId}/share", request);

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadFromJsonAsync<SharedLinkResponse>(token);

        return responseContent;
    }

    public async Task RevokeShareLinkAsync(string linkId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync("/api/share/" + linkId, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}