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
    private readonly UserAccessCache userAccessCache;

    public DanceHttpApiClient(IHttpClientFactory httpClientFactory,
        [FromKeyedServices(TokenStorage.PrimaryStorageKey)]ITokenProviderService primaryTokenProviderService,
        UserAccessCache userAccessCache)
    {
        this.primaryTokenProviderService = primaryTokenProviderService;
        this.userAccessCache = userAccessCache;
        this.httpClient = httpClientFactory.CreateClient(nameof(DanceHttpApiClient));
    }

    public async Task RenameVideoAsync(Guid videoId, string newName, CancellationToken cancellationToken = default)
    {
        var request = new RenameVideoRequest() { NewName = newName };
        using var response = await httpClient.PostAsJsonAsync($"/api/videos/{videoId}/rename", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteVideoAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"/api/videos/{videoId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public Task<GetUserAccessResponse> GetUserAccesses(CancellationToken cancellationToken = default) =>
        userAccessCache.GetOrCreateAsync(FetchUserAccesses, cancellationToken);

    private async Task<GetUserAccessResponse> FetchUserAccesses(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("/api/videos/accesses/my", cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<GetUserAccessResponse>(cancellationToken);
        return content ?? new GetUserAccessResponse
        {
            Assigned  = new GetUserAccessSet(),
            Available = new GetUserAccessSet(),
            Pending   = new ListUserAccessPending()
        };
    }

    public async Task RequestAccess(RequestAccessRequest accessRequest, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("/api/videos/accesses/request", accessRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        userAccessCache.Invalidate();
    }

    public async Task<PagedResponse<VideoFromGroupInformation>> GetVideosFromGroups(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"/api/groups/videos?page={page}&pageSize={pageSize}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<PagedResponse<VideoFromGroupInformation>>(cancellationToken);

        return content ?? new PagedResponse<VideoFromGroupInformation> { PageNumber = page, PageSize = pageSize };
    }

    public async Task<PagedResponse<VideoInformation>> GetVideosForEvent(Guid eventId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync($"/api/events/{eventId}/videos?page={page}&pageSize={pageSize}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<PagedResponse<VideoInformation>>(cancellationToken);

            return content ?? new PagedResponse<VideoInformation> { PageNumber = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error on getting videos for event {id}", eventId);
            throw;
        }
    }

    public async Task<RefreshUploadUrlResponse> RefreshUploadUrl(
        Guid videoId,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/videos/upload/{videoId}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<RefreshUploadUrlResponse>(cancellationToken);
        return content!;
    }

    public async Task<ProduceUploadUrlResponse?> GetUploadInformation(
        string fileName,
        string nameOfVideo,
        SharingWithType sharingWith,
        Guid? sharedWithId,
        DateTime recordedTimeUtc,
        CancellationToken cancellationToken = default
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

        var response = await httpClient.PostAsJsonAsync("/api/videos/upload", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<ProduceUploadUrlResponse>(cancellationToken);
        return content;
    }

    public async Task<Stream> GetStream(string videoBlobId, CancellationToken cancellationToken = default)
    {
        var responseMessage = await httpClient.GetAsync($"/api/videos/{videoBlobId}/stream", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        try
        {
            responseMessage.EnsureSuccessStatusCode();
            var stream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);
            return new ResponseDisposingStream(stream, responseMessage);
        }
        catch
        {
            responseMessage.Dispose();
            throw;
        }
    }

    public async Task<(Uri uri, string authToken)> GetVideoUri(string videoBlobId, CancellationToken cancellationToken = default)
    {
        var token = await primaryTokenProviderService
            .GetAccessTokenSilently()
            .WaitAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("A valid access token is required to play video.");

        var builder = new UriBuilder(httpClient.BaseAddress!)
        {
            Path = $"/api/videos/{videoBlobId}/stream"
        };

        return (builder.Uri, token);
    }

    public async Task CreateEvent(string eventName, DateTime eventDate, CancellationToken cancellationToken = default)
    {
        var body = new CreateNewEventRequest() { Event = new EventModel() { Date = eventDate, Name = eventName } };
        using var res = await httpClient.PostAsJsonAsync($"/api/events", body, cancellationToken);

        res.EnsureSuccessStatusCode();
    }

    public async Task<PagedResponse<VideoInformation>> GetMyVideos(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"/api/videos/my?page={page}&pageSize={pageSize}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var videos = await response.Content.ReadFromJsonAsync<PagedResponse<VideoInformation>>(cancellationToken);
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
