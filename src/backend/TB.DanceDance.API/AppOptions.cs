namespace TB.DanceDance.API;

public record AppOptions
{
    public const string Position = "AppOptions";
    public string AppWebsiteOrigin { get; set; }
}