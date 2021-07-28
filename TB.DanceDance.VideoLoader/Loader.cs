using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using MetadataExtractor;
using Microsoft.EntityFrameworkCore;
using NReco.VideoInfo;
using Serilog;
using TB.DanceDance.Data;
using TB.DanceDance.Data.Models;
using Directory = System.IO.Directory;

namespace TB.DanceDance.VideoLoader
{
    public class Loader : IDisposable
    {
        private readonly DanceType danceType;
        private readonly string blobConnectionString;
        private readonly ApplicationDbContext context;
        private BlobContainerClient container;

        public Loader(DanceType danceType, string databaseConnectionString, string blobConnectionString)
        {
            if (danceType == DanceType.NotSpecified)
                throw new ArgumentException("Dance type must be specified.");

            if (string.IsNullOrEmpty(databaseConnectionString))
                throw new ArgumentNullException(nameof(databaseConnectionString));

            if (string.IsNullOrEmpty(blobConnectionString))
                throw new ArgumentNullException(nameof(blobConnectionString));

            this.danceType = danceType;
            this.blobConnectionString = blobConnectionString;
            ApplicationDbContext.ConnectionString = databaseConnectionString;
            context = new ApplicationDbContext(new DbContextOptions<ApplicationDbContext>());
            ConfigureBlob();
        }

        public async Task LoadData(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                var files = Directory.GetFiles(folderPath);

                Console.WriteLine($"Are you sure to load {files.Length} files? We are going to set dance type to: {danceType} (y/N)");
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

        private void ConfigureBlob()
        {
            container = new BlobContainerClient(blobConnectionString, Constants.Infrastructure.VideoBlobContainerName);
            container.CreateIfNotExists();
        }

        private async Task LoadThemAll(ICollection<string> files)
        {
            int current = 0;
            foreach (var file in files)
            {
                await LoadFile(file);
                Log.Information("Loaded: {current}/{count}", current++, files.Count);
            }
        }

        private async Task LoadFile(string file)
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
                BlobId = guid,
                Name = Path.GetFileName(file),
                Type = danceType,
                CreationTimeUtc = f.LastWriteTimeUtc,
                MetadataAsJson = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(directories),
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

        private string GetValue(IEnumerable<MetadataExtractor.Directory> directories, string directoryName, string tagName)
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
