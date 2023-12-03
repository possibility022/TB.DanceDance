using FFMpegCore;
using Serilog;

namespace TB.DanceDance.Services.Converter.Deamon.FFmpegClient;
internal class FFmpegClientConverter
{
    public async Task ConvertAsync(string input, string output)
    {
        var args = FFMpegArguments
            .FromFileInput(input)
            .OutputToFile(output, overwrite: false, addArguments: options => options
                .WithVideoCodec("libvpx-vp9")
                .WithVideoBitrate(2000)
                .ForceFormat("webm"))
            .WithLogLevel(FFMpegCore.Enums.FFMpegLogLevel.Info)
            .NotifyOnError((m) =>
            {
                if (m != null && !m.StartsWith("frame="))
                    Log.Error(m);
            })
            .NotifyOnProgress(d => Log.Verbose("Progress: {0}", d))
            .NotifyOnOutput(m => Log.Information(m));

        await args.ProcessAsynchronously();
    }

    public async Task<(DateTime, TimeSpan)?> GetInfoAsync(string input)
    {
        var res = await FFProbe.AnalyseAsync(input);

        DateTime? creationTime = null;

        foreach (var video in res.VideoStreams)
        {
            creationTime = GetCreationTime(video.Tags);
            if (creationTime != null)
                return (creationTime.Value, res.Duration);
        }

        foreach(var audio in res.AudioStreams)
        {
            creationTime = GetCreationTime(audio.Tags);
            if (creationTime != null)
                return (creationTime.Value, res.Duration);
        }

        Log.Warning("Default creation date.");
        return (DateTime.Now, res.Duration);
    }

    private DateTime? GetCreationTime(Dictionary<string, string>? tags)
    {
        if (tags == null) 
            return null;

        if (tags.ContainsKey("creation_time"))
            return DateTime.Parse(tags["creation_time"]);
        return null;
    }
}
