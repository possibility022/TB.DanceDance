using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

string host = Environment.GetEnvironmentVariable("BlobStorageHostName") ?? "host.docker.internal";

Console.WriteLine("Connecting to: " + host);

string connectionString =
    "AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint="
    + $"http://{host}:10000/devstoreaccount1;QueueEndpoint="
    + $"http://{host}:10001/devstoreaccount1;TableEndpoint="
    + $"http://{host}:10002/devstoreaccount1;";

const string containerName = "videos";

const string backupVideoUrl =
    "https://test-videos.co.uk/vids/bigbuckbunny/mp4/h264/1080/Big_Buck_Bunny_1080_10s_5MB.mp4";

var videos = new[]
{
    "82b39019-d983-44ce-924a-f3fa2f651261",
    "412fcbe4-9dcc-435c-901f-58c9d71d3972",
    "fd040bc4-3d09-4b42-829f-b036a1875d53",
    "abab3514-39f0-47d6-ba16-f8ec6b532db4",
    "9ae02f12-e123-4548-931d-c5281b922bc5",
    "f91bded0-8de3-4cfd-bd79-1f6dbe5de5e6",
    "161ddf84-d0a9-488c-9f9a-948e79687fe7",
    "0dda7622-cca4-4918-8aaa-30edd8d623b8",
    "5c45fc7e-5697-495e-953a-637d1cef0869",
    "7c33e56e-6c66-433a-810b-f4863aea9915",
};

BlobServiceClient blobClient = new(connectionString);

Console.WriteLine("Setting cors");
var props = new BlobServiceProperties()
{
    Logging = new BlobAnalyticsLogging()
    {
        Version = "1.0" // has to be set. Otherwise, blob storage returns 400 (tested with azurite) 
    },
    Cors = (List<BlobCorsRule>)
    [
        new()
        {
            AllowedHeaders = "*",
            AllowedMethods = "GET,DELETE,PUT,OPTIONS",
            ExposedHeaders = "*",
            AllowedOrigins = "*",
            MaxAgeInSeconds = 200
        }
    ]
};


await blobClient.SetPropertiesAsync(props);

Console.WriteLine("Setup 'videostoconvert' container");
var videosToConvert = blobClient.GetBlobContainerClient("videostoconvert");
videosToConvert.CreateIfNotExists();

Console.WriteLine("Setup '{0}' container", containerName);
var videosContainer = blobClient.GetBlobContainerClient(containerName);
videosContainer.CreateIfNotExists();

using var httpClient = new HttpClient();
using var videoContent = new MemoryStream();
using var stream = await httpClient.GetStreamAsync(backupVideoUrl);
await stream.CopyToAsync(videoContent);
await videoContent.FlushAsync();


var upload = async (string id) =>
{
    var blob = videosContainer.GetBlobBaseClient(id);
    if (blob.Exists())
    {
        Console.WriteLine("{0} - Blob exists", id);
        return;
    }

    videoContent.Position = 0;
    Stream sourceStream = videoContent;
    
    videosContainer.UploadBlob(id, sourceStream);
    Console.WriteLine("Uploaded: {0}", id);
};


foreach (string video in videos)
{
    await upload(video);
}