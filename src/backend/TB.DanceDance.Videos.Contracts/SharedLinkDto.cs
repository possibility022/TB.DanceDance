namespace TB.DanceDance.Videos.Contracts;

public record SharedLinkDto
{
    public string Id { get; init; } = null!;
    public Guid VideoId { get; init; }
    public string SharedBy { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpireAt { get; init; }
    public bool IsRevoked { get; init; }
    public bool AllowComments { get; init; }
    public bool AllowAnonymousComments { get; init; }

    /// <summary>
    /// Details of the shared video. Populated for listing/lookup operations.
    /// </summary>
    public VideoDto? Video { get; init; }
}
