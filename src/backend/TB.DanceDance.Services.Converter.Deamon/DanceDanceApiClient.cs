using Azure.Storage.Blobs;
using System.Net.Http.Json;
using System.Text.Json;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.Services.Converter.Deamon.OAuthClient;

namespace TB.DanceDance.Services.Converter.Deamon;
internal class DanceDanceApiClient : IDanceDanceApiClient, IDisposable
{
    private readonly ApiHttpClient apiClient;
    private readonly HttpClient blobClient;
    private readonly JsonSerializerOptions serializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DanceDanceApiClient(ApiHttpClient apiClient, HttpClient blobClient)
    {
        this.apiClient = apiClient;
        this.blobClient = blobClient;
    }

    public async Task<VideoToTransformResponse?> GetNextVideoToConvertAsync(CancellationToken token)
    {
        var response = await apiClient.GetAsync("/api/converter/videos", token);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var contentStream = await response.Content.ReadAsStreamAsync(token);

        var videoToTransform = await System.Text.Json.JsonSerializer.DeserializeAsync<VideoToTransformResponse>(contentStream, serializationOptions, cancellationToken: token);

        if (videoToTransform == null)
            throw new NullReferenceException("Expected not null.");

        return videoToTransform;
    }

    public async Task GetVideoToConvertAsync(Stream target, Uri videoUrl, CancellationToken token)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        if (videoUrl is null)
            throw new ArgumentNullException(nameof(videoUrl));


        var blobResponse = await blobClient.GetAsync(videoUrl, HttpCompletionOption.ResponseHeadersRead, token);
        blobResponse.EnsureSuccessStatusCode();
        var videoContent = await blobResponse.Content.ReadAsStreamAsync(token);
        await videoContent.CopyToAsync(target, token);
    }

    public async Task UploadVideoToTransformInformation(UpdateVideoInfoRequest updateVideoInfoRequest, CancellationToken token)
    {
        var res = await apiClient.PostAsJsonAsync("/api/converter/videos", updateVideoInfoRequest, token);
        res.EnsureSuccessStatusCode();
    }

    public async Task UploadContent(Guid videoId, Stream content, CancellationToken token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/converter/videos/{videoId}/sas");

        var res = await apiClient.SendAsync(request, token);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<GetPublishSasResponse>(serializationOptions, cancellationToken: token);

        if (body == null)
            throw new NullReferenceException("Deserialized body is null.");

        var cloudBlockBlob = new BlobClient(new Uri(body.Sas));
        await cloudBlockBlob.UploadAsync(content, token);
    }

    public async Task PublishTransformedVideo(Guid videoId, CancellationToken token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/converter/videos/{videoId}/publish");

        var res = await apiClient.SendAsync(request, token);
        res.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        apiClient.Dispose();
        blobClient.Dispose();
    }
}
