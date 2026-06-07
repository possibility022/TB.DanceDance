#:package Azure.Storage.Blobs@12.26.0

// Manage blobs in the local Azurite emulator using the same SDK and well-known
// dev-storage credentials as the app (see ConnectionStrings:Blob in
// local_environment.dockercompose.yaml). A "file-based" C# app — no .csproj,
// run directly with `dotnet run <this file>`.
//
// Known containers in this app: videos, videostoconvert, thumbnails
// (src/backend/Infrastructure/Data/BlobStorage/BlobDataServiceFactory.cs).
//
// Usage:
//   dotnet run azurite-blob.cs containers
//   dotnet run azurite-blob.cs list <container> [prefix]
//   dotnet run azurite-blob.cs get <container> <blobName> <outFile>
//   dotnet run azurite-blob.cs put <container> <blobName> <inFile> [contentType]
//   dotnet run azurite-blob.cs delete <container> <blobName>
//   dotnet run azurite-blob.cs delete <container> --prefix <prefix> [--force]

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

const string ConnectionString =
    "AccountName=devstoreaccount1;" +
    "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
    "DefaultEndpointsProtocol=http;" +
    "BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;" +
    "QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;" +
    "TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

var serviceClient = new BlobServiceClient(ConnectionString);
var command = args[0].ToLowerInvariant();

try
{
    switch (command)
    {
        case "containers":
            await ListContainersAsync();
            break;

        case "list":
            RequireArgs(2, "list <container> [prefix]");
            await ListBlobsAsync(args[1], args.Length > 2 ? args[2] : null);
            break;

        case "get":
            RequireArgs(4, "get <container> <blobName> <outFile>");
            await DownloadBlobAsync(args[1], args[2], args[3]);
            break;

        case "put":
            RequireArgs(4, "put <container> <blobName> <inFile> [contentType]");
            await UploadBlobAsync(args[1], args[2], args[3], args.Length > 4 ? args[4] : null);
            break;

        case "delete":
            RequireArgs(3, "delete <container> <blobName> | delete <container> --prefix <prefix> [--force]");
            await DeleteAsync(args[1], args.Skip(2).ToArray());
            break;

        default:
            Console.Error.WriteLine($"Unknown command '{command}'.");
            PrintUsage();
            return 1;
    }
}
catch (Azure.RequestFailedException ex)
{
    Console.Error.WriteLine($"Azurite request failed: {ex.ErrorCode} ({ex.Status}) - {ex.Message}");
    Console.Error.WriteLine("Is azuriteStorage running and published on port 10000? (docker compose -f local_environment.dockercompose.yaml ps)");
    return 1;
}

return 0;

void RequireArgs(int min, string usage)
{
    if (args.Length < min)
        throw new ArgumentException($"Usage: dotnet run azurite-blob.cs {usage}");
}

void PrintUsage()
{
    Console.WriteLine("""
        Manage Azurite blobs locally (list / download / upload / replace / delete).

        Usage:
          dotnet run azurite-blob.cs containers
          dotnet run azurite-blob.cs list <container> [prefix]
          dotnet run azurite-blob.cs get <container> <blobName> <outFile>
          dotnet run azurite-blob.cs put <container> <blobName> <inFile> [contentType]
          dotnet run azurite-blob.cs delete <container> <blobName>
          dotnet run azurite-blob.cs delete <container> --prefix <prefix> [--force]

        Known containers: videos, videostoconvert, thumbnails
        """);
}

async Task ListContainersAsync()
{
    var found = false;
    await foreach (var container in serviceClient.GetBlobContainersAsync())
    {
        found = true;
        Console.WriteLine($"{container.Name,-20} lastModified={container.Properties.LastModified:u}");
    }
    if (!found) Console.WriteLine("(no containers)");
}

async Task ListBlobsAsync(string containerName, string? prefix)
{
    var container = serviceClient.GetBlobContainerClient(containerName);
    var found = false;
    await foreach (var blob in container.GetBlobsAsync(prefix: prefix))
    {
        found = true;
        Console.WriteLine($"{blob.Name,-50} {blob.Properties.ContentLength,12} bytes  {blob.Properties.ContentType,-25} {blob.Properties.LastModified:u}");
    }
    if (!found)
    {
        var suffix = prefix is null ? "" : $" matching prefix '{prefix}'";
        Console.WriteLine($"(no blobs in '{containerName}'{suffix})");
    }
}

async Task DownloadBlobAsync(string containerName, string blobName, string outFile)
{
    var blob = serviceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
    if (!await blob.ExistsAsync())
        throw new InvalidOperationException($"Blob '{blobName}' not found in container '{containerName}'.");

    var dir = Path.GetDirectoryName(Path.GetFullPath(outFile));
    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

    await blob.DownloadToAsync(outFile);
    var size = new FileInfo(outFile).Length;
    Console.WriteLine($"Saved {containerName}/{blobName} -> {outFile} ({size:N0} bytes)");
}

async Task UploadBlobAsync(string containerName, string blobName, string inFile, string? contentType)
{
    if (!File.Exists(inFile))
        throw new FileNotFoundException($"Local file not found: {inFile}");

    var container = serviceClient.GetBlobContainerClient(containerName);
    await container.CreateIfNotExistsAsync();
    var blob = container.GetBlobClient(blobName);

    var existedBefore = await blob.ExistsAsync();
    var options = contentType is null
        ? null
        : new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } };

    await using var stream = File.OpenRead(inFile);
    await blob.UploadAsync(stream, options ?? new BlobUploadOptions(), CancellationToken.None);

    var verb = existedBefore ? "Replaced" : "Uploaded";
    Console.WriteLine($"{verb} {inFile} -> {containerName}/{blobName} ({new FileInfo(inFile).Length:N0} bytes{(contentType is null ? "" : $", {contentType}")})");
}

async Task DeleteAsync(string containerName, string[] rest)
{
    var container = serviceClient.GetBlobContainerClient(containerName);
    var force = rest.Contains("--force");
    var prefixIndex = Array.IndexOf(rest, "--prefix");

    string[] targets;
    if (prefixIndex >= 0)
    {
        if (prefixIndex + 1 >= rest.Length)
            throw new ArgumentException("--prefix requires a value.");
        var prefix = rest[prefixIndex + 1];

        var names = new List<string>();
        await foreach (var blob in container.GetBlobsAsync(prefix: prefix))
            names.Add(blob.Name);
        targets = names.ToArray();

        if (targets.Length == 0)
        {
            Console.WriteLine($"No blobs in '{containerName}' match prefix '{prefix}' - nothing to do.");
            return;
        }
    }
    else
    {
        targets = [rest[0]];
    }

    if (!force)
    {
        Console.WriteLine($"Dry run - would delete {targets.Length} blob(s) from '{containerName}':");
        foreach (var name in targets) Console.WriteLine($"  {name}");
        Console.WriteLine();
        Console.WriteLine("Re-run with --force to actually delete.");
        return;
    }

    foreach (var name in targets)
    {
        var deleted = await container.GetBlobClient(name).DeleteIfExistsAsync();
        Console.WriteLine(deleted.Value ? $"Deleted {containerName}/{name}" : $"Not found: {containerName}/{name}");
    }
}
