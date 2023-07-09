using FFMpegCore.Pipes;
using FFMpegCore;
using Serilog;

namespace TB.DanceDance.Services.Converter.Deamon.FFmpegClient;
internal class Converter
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
            .NotifyOnProgress(d => Log.Information("Progress: {0}", d))
            .NotifyOnOutput(m => Log.Information(m));
        
        await args.ProcessAsynchronously();
    }
}
