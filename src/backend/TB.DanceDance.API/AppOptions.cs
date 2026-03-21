namespace TB.DanceDance.API;

public record AppOptions
{
    public const string Position = "AppOptions";
    public string AppWebsiteOrigin { get; set; }
}

public record OTelOptions
{
    public const string Position = "OTelOptions";
    public string OTelOrigin { get; set; }
    public string IngresKey { get; set; } = string.Empty;
}