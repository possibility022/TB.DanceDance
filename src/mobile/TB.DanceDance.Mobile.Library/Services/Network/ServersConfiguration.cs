namespace TB.DanceDance.Mobile.Library.Services.Network;

public sealed record ServersConfiguration
{
    public required Uri Primary { get; init; }

    public required Uri Secondary { get; init; }
    public required string HealthEndpoint { get; init; }
}