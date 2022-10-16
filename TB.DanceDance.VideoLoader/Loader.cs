using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using FFmpeg.NET;
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
        private readonly string ffmpgExecutionFile;
        private BlobContainerClient container;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ffmpgExecutionFile">Path to executable ffmpeg.exe. It is used to get video informations.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public Loader(string ffmpgExecutionFile)
        {
            var factory = new ApplicationDbContextFactory();
            context = factory.CreateDbContext(Array.Empty<string>());
            container = ConfigureBlob();

            context.Database.EnsureCreated();
            container.CreateIfNotExists();
            this.ffmpgExecutionFile = ffmpgExecutionFile ?? throw new ArgumentNullException(nameof(ffmpgExecutionFile));
        }

        public async Task LoadData(string folderPath, string searchPattern)
        {
            if (Directory.Exists(folderPath))
            {
                var files = Directory.GetFiles(folderPath, searchPattern);

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
            var initial = context.VideosInformation.Count() + 1;
            var all = initial + files.Count;
            int current = initial;
            foreach (var file in files)
            {
                await LoadFile(file, current);
                Log.Information("Loaded: {current}/{count}", current++, all);
            }
        }

        private async Task LoadFile(string file, int currentIndex)
        {
            Log.Information("Working on file {path}", file);
            string guid = Guid.NewGuid().ToString();
            var f = new FileInfo(file);

            (var recorded, var duration, var metadata) = await GetMetadataAsync(f, CancellationToken.None);

            var metadataAsJson = System.Text.Json.JsonSerializer.Serialize(metadata, options: new System.Text.Json.JsonSerializerOptions()
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
                MaxDepth = 15
            });

            context.VideosInformation.Add(new VideoInformation()
            {
                Id = currentIndex,
                BlobId = guid,
                Name = Path.GetFileName(file),
                CreationTimeUtc = recorded,
                MetadataAsJson = System.Text.Json.JsonSerializer.Serialize(metadataAsJson),
                Duration = duration
            });

            await context.SaveChangesAsync();


            Log.Information("Loading {guid}.", guid);

            var blobClient = container.GetBlobClient(guid);
            await blobClient.UploadAsync(File.OpenRead(file));

            Log.Information("{guid} loaded.", guid);
        }

        private async Task<(DateTime, TimeSpan?, object?)> GetMetadataAsync(FileInfo file, CancellationToken cancellationToken)
        {
            if (file.Extension == ".webm")
            {
                var metadata = await GetMetadataAsync(file.FullName, cancellationToken);

                dynamic customMetadata = new
                {
                    Video = metadata.VideoData,
                    Audio = metadata.AudioData,
                };

                var creationDate = GetDateTimeFromFileName(file.Name);

                return (creationDate, metadata.Duration, customMetadata);
            }
            else
            {
                // This does not work with webm converted by ffmpg
                using var stream = file.OpenRead();
                var directories = ImageMetadataReader.ReadMetadata(stream);
                foreach (var directory in directories)
                    foreach (var tag in directory.Tags)
                        Log.Information($"{directory.Name} - {tag.Name} = {tag.Description}");

                var duration = GetDurationTime(directories);

                return (file.LastWriteTime, duration, directories);
            }

        }

        private DateTime GetDateTimeFromFileName(string fileName)
        {
            // example: 20220209
            var span = fileName.AsSpan();

#if DEBUG

            var year = span.Slice(0, 4).ToString();
            var month = span.Slice(4, 2).ToString();
            var day = span.Slice(6, 2).ToString();

#else 

            var year = span.Slice(0, 4);
            var month = span.Slice(4, 2);
            var day = span.Slice(6, 2);
#endif

            return new DateTime(int.Parse(year), int.Parse(month), int.Parse(day));
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

        private Task<MetaData> GetMetadataAsync(string file, CancellationToken cancellationToken)
        {
            var media = new FFmpeg.NET.InputFile(file);

            var engine = new Engine(ffmpgExecutionFile);
            return engine.GetMetaDataAsync(media, cancellationToken);
        }
    }
}
