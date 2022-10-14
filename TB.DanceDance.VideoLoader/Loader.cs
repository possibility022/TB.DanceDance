using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using MetadataExtractor;
using Serilog;
using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.Db;
using TB.DanceDance.Data.Models;
using Directory = System.IO.Directory;

namespace TB.DanceDance.VideoLoader
{
    public class Loader : IDisposable
    {
        private readonly ApplicationDbContext context;
        private BlobContainerClient container;

        public Loader()
        {
            var factory = new ApplicationDbContextFactory();
            context = factory.CreateDbContext(Array.Empty<string>());
            container = ConfigureBlob();

            context.Database.EnsureCreated();
            container.CreateIfNotExists();
        }

        public async Task LoadData(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                var files = Directory.GetFiles(folderPath);

                Console.WriteLine($"Are you sure to load {files.Length} files?");
                var input = Console.ReadLine();
                if (input != null && input.Equals("y", StringComparison.CurrentCultureIgnoreCase))
                {
                    await LoadThemAll(files);
                }
                else
                {
                    Console.WriteLine("No? Well, then bye bye.");
                }
            }
            else
            {
                Log.Information("Directory does not exists.");
            }
        }

        private BlobContainerClient ConfigureBlob()
        {
            var connectionString = ApplicationBlobContainerFactory.TryGetConnectionStringFromEnvironmentVariables();
            if (connectionString == null)
                throw new Exception("connection string is null");

            container = new BlobContainerClient(connectionString, Constants.Infrastructure.VideoBlobContainerName);
            return container;
        }

        private async Task LoadThemAll(ICollection<string> files)
        {
            int current = 0;
            foreach (var file in files)
            {
                await LoadFile(file, current);
                Log.Information("Loaded: {current}/{count}", current++, files.Count);
            }
        }

        private async Task LoadFile(string file, int currentIndex)
        {
            Log.Information("Working on file {path}", file);
            string guid = Guid.NewGuid().ToString();
            var f = new FileInfo(file);

            var directories = ImageMetadataReader.ReadMetadata(file);
            foreach (var directory in directories)
                foreach (var tag in directory.Tags)
                    Log.Information($"{directory.Name} - {tag.Name} = {tag.Description}");

            var duration = GetDurationTime(directories);


            context.VideosInformation.Add(new VideoInformation()
            {
                Id = currentIndex,
                BlobId = guid,
                Name = Path.GetFileName(file),
                CreationTimeUtc = f.LastWriteTimeUtc,
                MetadataAsJson = System.Text.Json.JsonSerializer.Serialize(directories),
                Duration = duration
            });

            await context.SaveChangesAsync();


            Log.Information("Loading {guid}.", guid);

            var blobClient = container.GetBlobClient(guid);
            await blobClient.UploadAsync(File.OpenRead(file));

            Log.Information("{guid} loaded.", guid);
        }

        private TimeSpan? GetDurationTime(IEnumerable<MetadataExtractor.Directory> directories)
        {
            var v = GetValue(directories, "QuickTime Movie Header", "Duration");
            if (v != null)
            {
                if (TimeSpan.TryParse(v, CultureInfo.CurrentCulture, out var res))
                {
                    return res;
                }
            }

            return null;
        }

        private string? GetValue(IEnumerable<MetadataExtractor.Directory> directories, string directoryName, string tagName)
        {
            foreach (var directory in directories)
            {
                if (directory.Name != directoryName)
                    continue;

                foreach (var tag in directory.Tags)
                {
                    if (tag.Name == tagName)
                        return tag.Description;
                }
            }

            return null;
        }

        public void Dispose()
        {
            context?.Dispose();
        }
    }
}
