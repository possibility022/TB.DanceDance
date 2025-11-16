namespace TB.DanceDance.Services.Converter.Deamon.FFmpegClient;

internal interface IFFmpegClientConverter
{
    Task ConvertAsync(string input, string output);
    Task<(DateTime, TimeSpan)?> GetInfoAsync(string input);
}
