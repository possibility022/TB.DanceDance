namespace TB.DanceDance.Access.Contracts;

public class RequestedAccess
{
    public required string Name { get; set; }
    public required string RequestorFirstName { get; set; }
    public required string RequestorLastName { get; set; }
    public DateTime? WhenJoined { get; set; }
    public Guid RequestId { get; set; }
    public bool IsGroup { get; set; }
}
