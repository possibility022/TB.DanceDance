using Serilog;
using TB.DanceDance.Services.Converter.Deamon.FFmpegClient;

namespace TB.DanceDance.Services.Converter.Deamon;
internal class Deamon
{
    private readonly DanceDanceApiClient client;
    private readonly FFmpegClientConverter converter;

    public Deamon(DanceDanceApiClient client)
    {
        this.client = client;
        converter = new FFmpegClientConverter();
    }

    public async Task WorkAsync(CancellationToken token)
    {
        if (!Directory.Exists("D:\\temp\\convertingDeamon"))
            Directory.CreateDirectory("D:\\temp\\convertingDeamon");


        while (!token.IsCancellationRequested)
        {
            try
            {
                var converted = await ProcessNext(token);
                if (!converted)
                {
                    Log.Information("Leaving main loop.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in main execution path.");
            }
        }
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
        var inputVideo = $"D:\\temp\\convertingDeamon\\{guid}.source.{nextVideoToConvert.FileName}";
        var convertedFilePath = $"D:\\temp\\convertingDeamon\\{guid}.converted.webm";

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

        return true;
    }
}
