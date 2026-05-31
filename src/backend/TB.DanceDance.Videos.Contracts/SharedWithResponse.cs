namespace TB.DanceDance.Videos.Contracts;

public class SharedWithResponse
{
    public required Guid VideoId { get; init; }
    public required string UserId { get; init; }
    public Guid? EventId { get; set; }
    public Guid? GroupId { get; set; }
}