namespace Domain.Models;

public record UserRequests
{
    public required IReadOnlyCollection<Guid> Events { get; init; }
    public required IReadOnlyCollection<Guid> Groups { get; init; }
}