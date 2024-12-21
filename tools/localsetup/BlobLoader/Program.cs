using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

string host = Environment.GetEnvironmentVariable("BlobStorageHostName") ?? "host.docker.internal";

Console.WriteLine("Connecting to: " + host);

string connectionString =
    "AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint="
    + $"http://{host}:10000/devstoreaccount1;QueueEndpoint="
    + $"http://{host}:10001/devstoreaccount1;TableEndpoint="
    + $"http://{host}:10002/devstoreaccount1;";

const string containerName = "videos";

var videos = new[]
{
    ("82b39019-d983-44ce-924a-f3fa2f651261", "https://sample-videos.com/video321/mp4/720/big_buck_bunny_720p_1mb.mp4"),
    ("412fcbe4-9dcc-435c-901f-58c9d71d3972", "https://sample-videos.com/video321/mp4/480/big_buck_bunny_480p_1mb.mp4"),
    ("fd040bc4-3d09-4b42-829f-b036a1875d53", "https://sample-videos.com/video321/mp4/240/big_buck_bunny_240p_1mb.mp4"),
    ("abab3514-39f0-47d6-ba16-f8ec6b532db4", "https://sample-videos.com/video321/mp4/360/big_buck_bunny_360p_2mb.mp4"),
    ("9ae02f12-e123-4548-931d-c5281b922bc5", "https://sample-videos.com/video321/mp4/480/big_buck_bunny_480p_2mb.mp4"),
    ("f91bded0-8de3-4cfd-bd79-1f6dbe5de5e6", "https://sample-videos.com/video321/mp4/480/big_buck_bunny_480p_5mb.mp4"),
    ("161ddf84-d0a9-488c-9f9a-948e79687fe7", "https://sample-videos.com/video321/mp4/240/big_buck_bunny_240p_2mb.mp4"),
    ("0dda7622-cca4-4918-8aaa-30edd8d623b8", "https://sample-videos.com/video321/mp4/240/big_buck_bunny_240p_10mb.mp4")
};

BlobServiceClient blobClient = new(connectionString);
var container = blobClient.GetBlobContainerClient(containerName);
container.CreateIfNotExists();

using var httpClient = new HttpClient();

var upload = async ((string id, string uri) blobAndUri) =>
{
    var blob = container.GetBlobBaseClient(blobAndUri.id);
    if (blob.Exists())
    {
        Console.WriteLine("{0} - Blob exists", blobAndUri.id);
        return;
    }
    var stream = await httpClient.GetStreamAsync(blobAndUri.uri);
    container.UploadBlob(blobAndUri.id, stream);
    Console.WriteLine("Uploaded: {0} - {1}", blobAndUri.uri, blobAndUri.id);
};


foreach ((string, string) video in videos)
{
    await upload(video);
}