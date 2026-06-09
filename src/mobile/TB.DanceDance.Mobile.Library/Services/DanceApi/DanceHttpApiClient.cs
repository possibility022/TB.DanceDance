using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using TB.DanceDance.API.Contracts.Features.AccessManagement;
using TB.DanceDance.API.Contracts.Features.AccessManagement.Models;
using TB.DanceDance.API.Contracts.Features.Events;
using TB.DanceDance.API.Contracts.Features.Events.Models;
using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Features.Sharing;
using TB.DanceDance.API.Contracts.Features.Videos;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.Mobile.Library.Services.Auth;

namespace TB.DanceDance.Mobile.Library.Services.DanceApi;

public class DanceHttpApiClient : IDanceHttpApiClient
{
    private readonly ITokenProviderService primaryTokenProviderService;
    private readonly HttpClient httpClient;

    public DanceHttpApiClient(IHttpClientFactory httpClientFactory,

        [FromKeyedServices(TokenStorage.PrimaryStorageKey)]ITokenProviderService primaryTokenProviderService)
    {
        this.primaryTokenProviderService = primaryTokenProviderService;
        this.httpClient = httpClientFactory.CreateClient(nameof(DanceHttpApiClient));
    }

    public async Task RenameVideoAsync(Guid videoId, string newName)
    {
        var request = new RenameVideoRequest() { NewName = newName };
        var response = await httpClient.PostAsJsonAsync($"/api/videos/{videoId}/rename", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteVideoAsync(Guid videoId)
    {
        var response = await httpClient.DeleteAsync($"/api/videos/{videoId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<GetUserAccessResponse> GetUserAccesses()
    {
        var response = await httpClient.GetAsync("/api/videos/accesses/my");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<GetUserAccessResponse>();
        return content ?? new GetUserAccessResponse
        {
            Assigned  = new GetUserAccessSet(),
            Available = new GetUserAccessSet(),
            Pending   = new ListUserAccessPending()
        };
    }

    public async Task RequestAccess(RequestAccessRequest accessRequest)
    {
        var response = await httpClient.PostAsJsonAsync("/api/videos/accesses/request", accessRequest);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PagedResponse<VideoFromGroupInformation>> GetVideosFromGroups(int page, int pageSize)
    {
        var response = await httpClient.GetAsync($"/api/groups/videos?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<PagedResponse<VideoFromGroupInformation>>();

        return content ?? new PagedResponse<VideoFromGroupInformation> { PageNumber = page, PageSize = pageSize };
    }

    public async Task<PagedResponse<VideoInformation>> GetVideosForEvent(Guid eventId, int page, int pageSize)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/events/{eventId}/videos?page={page}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<PagedResponse<VideoInformation>>();

            return content ?? new PagedResponse<VideoInformation> { PageNumber = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error on getting videos for event {id}", eventId);
            throw;
        }
    }

    public async Task<RefreshUploadUrlResponse> RefreshUploadUrl(Guid videoId)
    {
        var response = await httpClient.GetAsync($"/api/videos/upload/{videoId}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<RefreshUploadUrlResponse>();
        return content!;
    }

    public async Task<ProduceUploadUrlResponse?> GetUploadInformation(
        string fileName,
        string nameOfVideo,
        SharingWithType sharingWith,
        Guid? sharedWithId,
        DateTime recordedTimeUtc
        )
    {
        ProduceUploadUrlRequest request = new()
        {
            SharingWithType = sharingWith,
            FileName = fileName,
            SharedWith = sharedWithId,
            NameOfVideo = nameOfVideo,
            RecordedTimeUtc = recordedTimeUtc
        };

        var response = await httpClient.PostAsJsonAsync("/api/videos/upload", request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<ProduceUploadUrlResponse>();
        return content;
    }

    public async Task<Stream> GetStream(string videoBlobId)
    {
        var responseMessage = await httpClient.GetAsync($"/api/videos/{videoBlobId}/stream", HttpCompletionOption.ResponseHeadersRead);
        responseMessage.EnsureSuccessStatusCode();

        return await responseMessage.Content.ReadAsStreamAsync();
    }

    public (Uri uri, string authToken) GetVideoUri(string videoBlobId)
    {
        var token = primaryTokenProviderService.GetValidAccessTokenNoFetch();

        var builder = new UriBuilder(httpClient.BaseAddress!)
        {
            Path = $"/api/videos/{videoBlobId}/stream"
        };

        return (builder.Uri, token!);
    }

    public async Task CreateEvent(string eventName, DateTime eventDate)
    {
        var body = new CreateNewEventRequest() { Event = new EventModel() { Date = eventDate, Name = eventName } };
        var res = await httpClient.PostAsJsonAsync($"/api/events", body);

        res.EnsureSuccessStatusCode();
    }

    public async Task<PagedResponse<VideoInformation>> GetMyVideos(int page, int pageSize)
    {
        var response = await httpClient.GetAsync($"/api/videos/my?page={page}&pageSize={pageSize}", CancellationToken.None);
        response.EnsureSuccessStatusCode();

        var videos = await response.Content.ReadFromJsonAsync<PagedResponse<VideoInformation>>();
        return videos ?? new PagedResponse<VideoInformation> { PageNumber = page, PageSize = pageSize };
    }

    public async Task<SharedLinkResponse?> GetSharingLinkAsync(Guid videoId, CancellationToken token = default)
    {
        CreateSharedLinkRequest request = new()
        {
            ExpirationDays = 7
        };

        var response = await httpClient.PostAsJsonAsync($"/api/videos/{videoId}/share", request, token);

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
