using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TB.DanceDance.Services.Converter.Deamon.FFmpegClient;

namespace TB.DanceDance.Services.Converter.Deamon;
internal sealed class Deamon : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ProgramConfig programConfig;

    public Deamon(IServiceScopeFactory scopeFactory, ProgramConfig programConfig)
    {
        this.scopeFactory = scopeFactory;
        this.programConfig = programConfig;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        if (!Directory.Exists(programConfig.WorkDir))
            Directory.CreateDirectory(programConfig.WorkDir);


        while (!token.IsCancellationRequested)
        {
            try
            {
                var converted = await ProcessNext(token);
                if (!converted)
                {
                    var delay = programConfig.DelayInMinutes * 1000 * 60;
                    Log.Information("Waiting till next run. Delay in minutes: {0}", programConfig.DelayInMinutes);

                    await Task.Delay(delay, token);
                }
            }
            catch(TaskCanceledException ex)
            {
                Log.Information(ex, "Task cancelled.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in main execution path. Awaiting 10 seconds.");
                await Task.Delay(10000, token);
            }
        }
    }

    private async Task<bool> ProcessNext(CancellationToken token)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<IDanceDanceApiClient>();
        var converter = scope.ServiceProvider.GetRequiredService<IFFmpegClientConverter>();

        Log.Information("Getting next video.");
        var response = await client.GetNextVideoToConvertAsync(token);

        if (response.VideoExists is false)
        {
            Log.Information("Nothing to convert.");
            return false;
        }

        var nextVideoToConvert = response.VideoToTransform!;

        Log.Information("Video to convert {0}", nextVideoToConvert.Id);

        var guid = nextVideoToConvert.Id;
        var inputVideo = Path.Combine(programConfig.WorkDir, $"{guid}.source.{nextVideoToConvert.FileName}");
        var convertedFilePath = Path.Combine(programConfig.WorkDir, $"{guid}.converted.webm");

        using (var file = File.Open(inputVideo, FileMode.Create))
        {
            Log.Information("Getting video content into {0}.", inputVideo);
            await client.GetVideoToConvertAsync(file, new Uri(nextVideoToConvert.Sas), token);
        }

        Log.Information("Getting video information.");
        var info = await converter.GetInfoAsync(inputVideo);
        Log.Information("Updating video informations.");
        await client.UploadVideoToTransformInformation(new TB.DanceDance.API.Contracts.Features.Videos.Converter.UpdateVideoInfoRequest()
        {
            Duration = info.Value.Item2,
            RecordedDateTime = info.Value.Item1,
            Metadata = new byte[0],
            VideoId = nextVideoToConvert.Id
        }, token);

        Log.Information("Converting video.");
        await converter.ConvertAsync(inputVideo, convertedFilePath);
        
        // Ensure the stream is disposed before deleting the file on Windows
        using (var convertedVideo = File.OpenRead(convertedFilePath))
        {
            Log.Information("Sending content");
            await client.UploadContent(nextVideoToConvert.Id, convertedVideo, token);
        }

        Log.Information("Publishing video.");
        await client.PublishTransformedVideo(nextVideoToConvert.Id, token);

        if (File.Exists(inputVideo))
            File.Delete(inputVideo);

        if (File.Exists(convertedFilePath))
            File.Delete(convertedFilePath);

        return true;
    }
}
