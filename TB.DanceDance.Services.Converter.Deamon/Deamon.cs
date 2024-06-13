using Microsoft.Extensions.Hosting;
using Serilog;
using TB.DanceDance.Services.Converter.Deamon.FFmpegClient;

namespace TB.DanceDance.Services.Converter.Deamon;
internal sealed class Deamon : BackgroundService
{
    private readonly DanceDanceApiClient client;
    private readonly FFmpegClientConverter converter;

    public Deamon(DanceDanceApiClient client)
    {
        this.client = client;
        converter = new FFmpegClientConverter();
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
                    var delay = GetDelayTillnextExecution();
                    Log.Information("Waiting till next run. Delay: {0}", delay.ToString(@"hh\:mm"));
                    
                    await Task.Delay(delay, token);
                }
            }
            catch(TaskCanceledException ex)
            {
                Log.Information(ex, "Task cancelled.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in main execution path.");
            }
        }
    }

    private TimeSpan GetDelayTillnextExecution()
    {
        var nextStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, ProgramConfig.Instance.HourOfExecution, 0, 0);
        if (DateTime.Now.AddMinutes(1) > nextStart)
            nextStart = nextStart.AddDays(1);

        var delay = nextStart - DateTime.Now;

        return delay;
    }

    private async Task<bool> ProcessNext(CancellationToken token)
    {
        Log.Information("Getting next video.");
        var nextVideoToConvert = await client.GetNextVideoToConvertAsync(token);

        if (nextVideoToConvert == null)
        {
            Log.Information("Nothing to convert.");
            return false;
        }

        Log.Information("Video to convert {0}", nextVideoToConvert.Id);

        var guid = nextVideoToConvert.Id;
        var inputVideo = Path.Combine(ProgramConfig.Instance.WorkDir, $"{guid}.source.{nextVideoToConvert.FileName}");
        var convertedFilePath = Path.Combine(ProgramConfig.Instance.WorkDir, $"{guid}.converted.webm");

        using (var file = File.Open(inputVideo, FileMode.Create))
        {
            Log.Information("Getting video content into {0}.", inputVideo);
            await client.GetVideoToConvertAsync(file, new Uri(nextVideoToConvert.Sas), token);
        }

        Log.Information("Getting video information.");
        var info = await converter.GetInfoAsync(inputVideo);
        Log.Information("Updating video informations.");
        await client.UploadVideoToTransformInformations(new TB.DanceDance.API.Contracts.Requests.UpdateVideoInfoRequest()
        {
            Duration = info.Value.Item2,
            RecordedDateTime = info.Value.Item1,
            Metadata = new byte[0],
            VideoId = nextVideoToConvert.Id
        }, token);

        Log.Information("Converting video.");
        await converter.ConvertAsync(inputVideo, convertedFilePath);
        using var convertedVideo = File.OpenRead(convertedFilePath);

        Log.Information("Sending content");
        await client.UploadContent(nextVideoToConvert.Id, convertedVideo);

        Log.Information("Publishing video.");
        await client.PublishTransformedVideo(nextVideoToConvert.Id);

        if (File.Exists(inputVideo))
            File.Delete(inputVideo);

        if (File.Exists(convertedFilePath))
            File.Delete(convertedFilePath);

        return true;
    }
}
