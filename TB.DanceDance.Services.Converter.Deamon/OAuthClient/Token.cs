namespace TB.DanceDance.Services.Converter.Deamon;

public record Token
{
    public required string AccessToken { get; init; }
    public required string Schema { get; init; }
}
