using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TB.DanceDance.Services.Converter.Deamon.FFmpegClient;

namespace TB.DanceDance.Services.Converter.Deamon;
internal sealed class Deamon : BackgroundService
{
    private readonly IDanceDanceApiClient client;
    private readonly IFFmpegClientConverter converter;
    private readonly ILogger<Deamon> logger;

    public Deamon(IDanceDanceApiClient client, IFFmpegClientConverter converter, ILogger<Deamon> logger)
    {
        this.client = client;
        this.converter = converter;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        if (!Directory.Exists(ProgramConfig.Instance.WorkDir))
            Directory.CreateDirectory(ProgramConfig.Instance.WorkDir);


        while (!token.IsCancellationRequested)
        {
            try
            {
                var converted = await ProcessNext(token);
                if (!converted)
                {
                    var delay = ProgramConfig.Instance.DelayInMinutes * 1000 * 60;
                    logger.LogInformation("Waiting till next run. Delay in minutes: {0}", ProgramConfig.Instance.DelayInMinutes);
                    
                    await Task.Delay(delay, token);
                }
            }
            catch(TaskCanceledException ex)
            {
                logger.LogInformation(ex, "Task cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in main execution path. Awaiting 10 seconds.");
                await Task.Delay(10000, token);
            }
        }
    }

    private async Task<bool> ProcessNext(CancellationToken token)
    {
        logger.LogInformation("Getting next video.");
        var nextVideoToConvert = await client.GetNextVideoToConvertAsync(token);

        if (nextVideoToConvert == null)
        {
            logger.LogInformation("Nothing to convert.");
            return false;
        }

        logger.LogInformation("Video to convert {0}", nextVideoToConvert.Id);

        var guid = nextVideoToConvert.Id;
        var inputVideo = Path.Combine(ProgramConfig.Instance.WorkDir, $"{guid}.source.{nextVideoToConvert.FileName}");
        var convertedFilePath = Path.Combine(ProgramConfig.Instance.WorkDir, $"{guid}.converted.webm");

        using (var file = File.Open(inputVideo, FileMode.Create))
        {
            logger.LogInformation("Getting video content into {0}.", inputVideo);
            await client.GetVideoToConvertAsync(file, new Uri(nextVideoToConvert.Sas), token);
        }

        logger.LogInformation("Getting video information.");
        var info = await converter.GetInfoAsync(inputVideo);
        logger.LogInformation("Updating video informations.");
        await client.UploadVideoToTransformInformation(new TB.DanceDance.API.Contracts.Requests.UpdateVideoInfoRequest()
        {
            Duration = info.Value.Item2,
            RecordedDateTime = info.Value.Item1,
            Metadata = new byte[0],
            VideoId = nextVideoToConvert.Id
        }, token);

        logger.LogInformation("Converting video.");
        await converter.ConvertAsync(inputVideo, convertedFilePath);
        
        // Ensure the stream is disposed before deleting the file on Windows
        using (var convertedVideo = File.OpenRead(convertedFilePath))
        {
            logger.LogInformation("Sending content");
            await client.UploadContent(nextVideoToConvert.Id, convertedVideo, token);
        }

        logger.LogInformation("Publishing video.");
        await client.PublishTransformedVideo(nextVideoToConvert.Id, token);

        if (File.Exists(inputVideo))
            File.Delete(inputVideo);

        if (File.Exists(convertedFilePath))
            File.Delete(convertedFilePath);

        return true;
    }
}
