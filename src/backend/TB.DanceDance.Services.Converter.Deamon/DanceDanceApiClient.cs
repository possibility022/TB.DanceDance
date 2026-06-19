using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Net.Http.Json;
using System.Text.Json;
using TB.DanceDance.API.Contracts.Features.Videos.Converter;
using TB.DanceDance.Services.Converter.Deamon.OAuthClient;

namespace TB.DanceDance.Services.Converter.Deamon;
internal class DanceDanceApiClient : IDanceDanceApiClient
{
    private readonly ApiHttpClient apiClient;
    private readonly HttpClient blobClient;
    private readonly ProgramConfig config;
    private readonly JsonSerializerOptions serializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DanceDanceApiClient(ApiHttpClient apiClient, HttpClient blobClient, ProgramConfig config)
    {
        this.apiClient = apiClient;
        this.blobClient = blobClient;
        this.config = config;
    }

    private Uri RewriteBlobUrl(Uri url)
    {
        if (string.IsNullOrEmpty(config.BlobEndpointOverride))
            return url;
        var target = new Uri(config.BlobEndpointOverride);
        var builder = new UriBuilder(url) { Scheme = target.Scheme, Host = target.Host, Port = target.Port };
        return builder.Uri;
    }

    public async Task<VideoToTransformResponse> GetNextVideoToConvertAsync(CancellationToken token)
    {
        var response = await apiClient.GetAsync("/api/converter/videos", token);

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


        // SAS URLs from the API embed the blob endpoint hostname configured on the API side,
        // which is 127.0.0.1 in development. Inside Docker, 127.0.0.1 is the container
        // itself, not Azurite. BlobEndpointOverride lets docker-compose remap to the
        // internal Docker service name (e.g. azuriteStorage) without changing the SAS signature.
        var blobResponse = await blobClient.GetAsync(RewriteBlobUrl(videoUrl), HttpCompletionOption.ResponseHeadersRead, token);
        blobResponse.EnsureSuccessStatusCode();
        var videoContent = await blobResponse.Content.ReadAsStreamAsync(token);
        await videoContent.CopyToAsync(target, token);
    }

    public async Task UploadVideoToTransformInformation(UpdateVideoInfoRequest updateVideoInfoRequest, CancellationToken token)
    {
        var res = await apiClient.PostAsJsonAsync("/api/converter/videos", updateVideoInfoRequest, token);
        
        if (res.IsSuccessStatusCode)
            return;
        
        var responseContent = await res.Content.ReadAsStringAsync(token);
        throw new Exception("Failed to update video info: " + responseContent + "HTTP Code:" + res.StatusCode);
    }

    public async Task UploadContent(Guid videoId, Stream content, CancellationToken token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/converter/videos/{videoId}/sas");

        var res = await apiClient.SendAsync(request, token);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<GetPublishSasResponse>(serializationOptions, cancellationToken: token);

        if (body == null)
            throw new NullReferenceException("Deserialized body is null.");

        // Same host rewrite as in GetVideoToConvertAsync — SAS upload URL may also contain
        // 127.0.0.1 from the API's development blob connection string.
        var cloudBlockBlob = new BlobClient(RewriteBlobUrl(new Uri(body.Sas)));
        await cloudBlockBlob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "video/webm" }
        }, token);
    }

    public async Task PublishTransformedVideo(Guid videoId, CancellationToken token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/converter/videos/{videoId}/publish");

        var res = await apiClient.SendAsync(request, token);
        res.EnsureSuccessStatusCode();
    }

    public async Task<VideoToThumbnailResponse> GetNextVideoForThumbnailAsync(CancellationToken token)
    {
        var response = await apiClient.GetAsync("/api/converter/thumbnails", token);
        response.EnsureSuccessStatusCode();

        var contentStream = await response.Content.ReadAsStreamAsync(token);
        var result = await System.Text.Json.JsonSerializer.DeserializeAsync<VideoToThumbnailResponse>(contentStream, serializationOptions, cancellationToken: token);

        if (result == null)
            throw new NullReferenceException("Expected not null.");

        return result;
    }

    public async Task UploadThumbnail(Guid videoId, Stream content, CancellationToken token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/converter/videos/{videoId}/thumbnail/sas");

        var res = await apiClient.SendAsync(request, token);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<GetThumbnailSasResponse>(serializationOptions, cancellationToken: token);

        if (body == null)
            throw new NullReferenceException("Deserialized body is null.");

        var cloudBlockBlob = new BlobClient(RewriteBlobUrl(new Uri(body.Sas)));
        await cloudBlockBlob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" }
        }, token);
    }

    public async Task PublishThumbnail(Guid videoId, CancellationToken token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/converter/videos/{videoId}/thumbnail/publish");

        var res = await apiClient.SendAsync(request, token);
        res.EnsureSuccessStatusCode();
    }
}
