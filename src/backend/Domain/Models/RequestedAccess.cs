namespace Domain.Models;
public class RequestedAccess
{
    public required string Name { get; set; }
    public required string RequestorFirstName { get; set; }
    public required string RequestorLastName { get; set; }

    /// <summary>
    /// When joined to group. Required for group. Not required for event.
    /// </summary>
    public DateTime? WhenJoined { get; set; }

    public Guid RequestId { get; set; }
    
    /// <summary>
    /// When true - it is a group. Otherwise, event.
    /// </summary>
    public bool IsGroup { get; set; }
}
