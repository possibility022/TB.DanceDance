using System.Net.Http.Json;
using System.Text.Json;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;

namespace TB.DanceDance.Services.Converter.Deamon;
internal class DanceDanceApiClient : IDisposable
{
    private readonly HttpClient apiClient;
    private readonly HttpClient blobClient;

    public DanceDanceApiClient(HttpClient apiClient, HttpClient blobClient)
    {
        this.apiClient = apiClient;
        this.blobClient = blobClient;
    }

    public async Task<VideoToTransform?> GetNextVideoToConvertAsync(CancellationToken token)
    {
        var response = await apiClient.GetAsync("/api/converter/video");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var contentStream = await response.Content.ReadAsStreamAsync();

        var videoToTransform = await System.Text.Json.JsonSerializer.DeserializeAsync<VideoToTransform>(contentStream, new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }, cancellationToken: token);

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


        var blobResponse = await blobClient.GetAsync(videoUrl);
        blobResponse.EnsureSuccessStatusCode();
        var videoContent = blobResponse.Content.ReadAsStream();
        await videoContent.CopyToAsync(target);
    }

    public async Task UploadVideoToTransformInformations(UpdateVideoInfoRequest updateVideoInfoRequest, CancellationToken token)
    {
        var res  = await apiClient.PostAsJsonAsync("/api/converter/video", updateVideoInfoRequest, token);
        res.EnsureSuccessStatusCode();
    }

    public async Task PublishTransformedVideo(Guid videoId, Stream source)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/converter/video/upload?videoId={videoId}")
        {
            Content = new StreamContent(source)
        };

        var res = await apiClient.SendAsync(request);
        res.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        apiClient.Dispose();
        blobClient.Dispose();
    }
}
