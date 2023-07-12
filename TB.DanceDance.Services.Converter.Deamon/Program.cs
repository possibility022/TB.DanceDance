// See https://aka.ms/new-console-template for more information
using Serilog;
using TB.DanceDance.Services.Converter.Deamon;
using TB.DanceDance.Services.Converter.Deamon.FFmpegClient;
using TB.DanceDance.Services.Converter.Deamon.OAuthClient;

ProgramConfig.Configure();

using var oauthClient = new HttpClient()
{
    BaseAddress = new Uri(ProgramConfig.Settings.OAuthOrigin)
};

var tokenProvider = new TokenProvider(oauthClient, ProgramConfig.TokenProviderOptions);

var handler = new TokenHttpHandler(tokenProvider);

using var apiHttpClient = new HttpClient(handler)
{
    BaseAddress = new Uri(ProgramConfig.Settings.ApiOrigin)
};

using var defaultHttpClient = new HttpClient();


var converter = new Converter();
var client = new DanceDanceApiClient(apiHttpClient, defaultHttpClient);

if (!Directory.Exists("D:\\temp\\convertingDeamon"))
    Directory.CreateDirectory("D:\\temp\\convertingDeamon");

CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
CancellationToken token = cancellationTokenSource.Token;

var task = Task.Run(async () =>
{
    while (!token.IsCancellationRequested)
    {
        Log.Information("Getting next video.");
        var nextVideoToConvert = await client.GetNextVideoToConvertAsync(token);

        if (nextVideoToConvert == null)
        {
            Log.Information("Nothing to convert. Waiting 5 min.");
            await Task.Delay(5 * 60 * 1000);
            continue;
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

        Log.Information("Publishing video.");
        await client.PublishTransformedVideo(nextVideoToConvert.Id, convertedVideo);
    }
});

try
{
    await task;
}
catch (Exception ex)
{
    Log.Error(ex, "Error in main execution path.");
}

