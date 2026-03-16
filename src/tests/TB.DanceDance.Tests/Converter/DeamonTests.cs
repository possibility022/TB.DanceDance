using NSubstitute;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.Services.Converter.Deamon;
using TB.DanceDance.Services.Converter.Deamon.FFmpegClient;

namespace TB.DanceDance.Tests.Converter;

public class DeamonTests
{
    private readonly IDanceDanceApiClient api = Substitute.For<IDanceDanceApiClient>();
    private readonly IFFmpegClientConverter ffmpeg = Substitute.For<IFFmpegClientConverter>();
    private readonly Deamon deamon;
    private readonly string tempDir;

    public DeamonTests()
    {
        // Setup temporary work directory
        tempDir = Path.Combine(Path.GetTempPath(), "dd-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        ProgramConfig.Instance.WorkDir = tempDir;

        deamon = new Deamon(api, ffmpeg);
    }

    [Fact]
    public async Task ExecuteAsync_NoVideo_ExitsOnCancellation()
    {
        // Arrange
        api.GetNextVideoToConvertAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<VideoToTransformResponse?>(null));

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        await deamon.StartAsync(cts.Token);
        await Task.Delay(10, TestContext.Current.CancellationToken);
        await deamon.StopAsync(CancellationToken.None);

        // Assert
        await api.Received(1).GetNextVideoToConvertAsync(Arg.Any<CancellationToken>());
        await ffmpeg.DidNotReceive().ConvertAsync(Arg.Any<string>(), Arg.Any<string>());
        await api.DidNotReceive().UploadContent(Arg.Any<Guid>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_FullFlow_ProcessesAndCleansUp()
    {
        // Arrange
        var id = Guid.NewGuid();
        var video = new VideoToTransformResponse
        {
            Id = id,
            FileName = "video.mp4",
            Sas = "https://example/blob"
        };

        // First call returns a video, second returns null so loop will go to delay and we cancel
        api.GetNextVideoToConvertAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<VideoToTransformResponse?>(video), Task.FromResult<VideoToTransformResponse?>(null));

        // Download writes the input file content
        api.GetVideoToConvertAsync(Arg.Any<Stream>(), Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var stream = ci.ArgAt<Stream>(0);
                var bytes = new byte[] { 1, 2, 3 };
                return stream.WriteAsync(bytes, 0, bytes.Length, ci.ArgAt<CancellationToken>(2));
            });

        // FFmpeg info
        ffmpeg.GetInfoAsync(Arg.Any<string>())
            .Returns(Task.FromResult<(DateTime, TimeSpan)?>(new(DateTime.UtcNow, TimeSpan.FromSeconds(5))));

        // FFmpeg convert writes the output file so client can open it
        var outputBytes = new byte[] { 9, 8, 7 };
        ffmpeg.ConvertAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci =>
            {
                var outputPath = ci.ArgAt<string>(1);
                File.WriteAllBytes(outputPath, outputBytes);
                return Task.CompletedTask;
            });

        // Capture uploads
        byte[]? uploaded = null;
        await api.UploadVideoToTransformInformation(Arg.Any<UpdateVideoInfoRequest>(), Arg.Any<CancellationToken>());
        api.UploadContent(id, Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                using var ms = new MemoryStream();
                ci.ArgAt<Stream>(1).CopyTo(ms);
                uploaded = ms.ToArray();
                return Task.CompletedTask;
            });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        // Act
        await deamon.StartAsync(cts.Token);
        await Task.Delay(200, TestContext.Current.CancellationToken);
        await deamon.StopAsync(CancellationToken.None);

        // Assert
        await api.Received(2).GetNextVideoToConvertAsync(Arg.Any<CancellationToken>());
        await api.Received(1).GetVideoToConvertAsync(Arg.Any<Stream>(), Arg.Any<Uri>(), Arg.Any<CancellationToken>());
        await ffmpeg.Received(1).GetInfoAsync(Arg.Any<string>());
        await ffmpeg.Received(1).ConvertAsync(Arg.Any<string>(), Arg.Any<string>());
        await api.Received(1).UploadVideoToTransformInformation(Arg.Is<UpdateVideoInfoRequest>(r => r.VideoId == id), Arg.Any<CancellationToken>());
        await api.Received(1).UploadContent(id, Arg.Any<Stream>(), Arg.Any<CancellationToken>());
        await api.Received(1).PublishTransformedVideo(id, Arg.Any<CancellationToken>());

        Assert.NotNull(uploaded);
        Assert.Equal(outputBytes, uploaded);
        // Ensure expected files are cleaned up
        var inputPath = Path.Combine(tempDir, $"{id}.source.video.mp4");
        var outputPath = Path.Combine(tempDir, $"{id}.converted.webm");
        Assert.False(File.Exists(inputPath));
        Assert.False(File.Exists(outputPath));
    }
}