using FFmpeg.NET;
using System.Globalization;
using MetadataExtractor;
using TB.DanceDance.Data.MongoDb.Models;

namespace TB.DanceDance.Services
{
    public class VideoFileLoader : IVideoFileLoader
    {
        private readonly string ffmpgExecutionFile;

        public VideoFileLoader(string ffmpgExecutionFile)
        {
            this.ffmpgExecutionFile = ffmpgExecutionFile ?? throw new ArgumentNullException(nameof(ffmpgExecutionFile));
        }

        public async Task<VideoInformation> CreateRecord(string filePath)
        {
            string guid = Guid.NewGuid().ToString();
            var f = new FileInfo(filePath);

            (var recorded, var duration, var metadata) = await GetMetadataAsync(f, CancellationToken.None);

            var metadataAsJson = System.Text.Json.JsonSerializer.Serialize(metadata, options: new System.Text.Json.JsonSerializerOptions()
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
                MaxDepth = 15
            });

            var videoInfo = new VideoInformation()
            {
                BlobId = guid,
                Name = Path.GetFileName(filePath),
                RecordedTimeUtc = recorded,
                MetadataAsJson = System.Text.Json.JsonSerializer.Serialize(metadataAsJson),
                Duration = duration
            };

            return videoInfo;
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

                var duration = GetDurationTime(directories);

                return (file.LastWriteTime, duration, directories);
            }

        }

        private Task<MetaData> GetMetadataAsync(string file, CancellationToken cancellationToken)
        {
            var media = new FFmpeg.NET.InputFile(file);

            var engine = new Engine(ffmpgExecutionFile);
            return engine.GetMetaDataAsync(media, cancellationToken);
        }

        private DateTime GetDateTimeFromFileName(string fileName)
        {
            // example: 20220209
            var span = fileName.AsSpan();

            var year = span.Slice(0, 4);
            var month = span.Slice(4, 2);
            var day = span.Slice(6, 2);

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
    }
}
