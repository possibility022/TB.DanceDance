// See https://aka.ms/new-console-template for more information
using Serilog;
using TB.DanceDance.Services.Converter.Deamon;
using TB.DanceDance.Services.Converter.Deamon.FFmpegClient;
using TB.DanceDance.Services.Converter.Deamon.OAuthClient;

ProgramConfig.Configure();

using var oauthClient = new HttpClient()
{
    BaseAddress = new Uri("https://localhost:7068/")
};

var tokenProvider = new TokenProvider(oauthClient, ProgramConfig.TokenProviderOptions);

var handler = new TokenHttpHandler(tokenProvider);

using var apiHttpClient = new HttpClient(handler)
{
    BaseAddress = new Uri("https://localhost:7068")
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
        var nextVideoToConvert = await client.GetNextVideoToConvertAsync(token);

        if (nextVideoToConvert == null)
        {
            Log.Information("Nothing to convert. Waiting 5 min.");
            await Task.Delay(5 * 60 * 1000);
            continue;
        }

        var guid = Guid.NewGuid();
        var filePath = $"D:\\temp\\convertingDeamon\\{guid}.source.{nextVideoToConvert.FileName}";
        var convertedFilePath = $"D:\\temp\\convertingDeamon\\{guid}.converted.webm";


        using (var file = File.Open(filePath, FileMode.OpenOrCreate))
        {
            await client.GetVideoToConvertAsync(file, new Uri(nextVideoToConvert.Sas), token);
        }

        var info = await converter.GetInfoAsync(filePath);
        await client.UploadVideoToTransformInformations(new TB.DanceDance.API.Contracts.UpdateVideoInfoRequest()
        {
            Duration = info.Value.Item2,
            RecordedDateTime = info.Value.Item1,
            Metadata = new byte[0],
            VideoId = nextVideoToConvert.Id
        }, token);

        await converter.ConvertAsync(filePath, convertedFilePath);
        using var convertedVideo = File.OpenRead(convertedFilePath);
        await client.PublishTransformedVideo(nextVideoToConvert.Id, convertedVideo);
    }
});

await task;

