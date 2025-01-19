using System.Net.Http.Json;
using TB.DanceDance.API.Contracts.Responses;

namespace TB.DanceDance.Mobile.Services.DanceApi;

public class DanceHttpApiClient
{
    private readonly HttpClient httpClient;

    public DanceHttpApiClient(IHttpClientFactory httpClientFactory)
    {
        this.httpClient = httpClientFactory.CreateClient(nameof(DanceHttpApiClient));
    }

    public async Task<UserEventsAndGroupsResponse?> GetUserEvents()
    {
        var response = await httpClient.GetAsync("/api/videos/accesses/my");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<UserEventsAndGroupsResponse>();
        return content;
    }
}