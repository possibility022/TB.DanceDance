namespace Domain.Models;
public class AccessRequest
{
    public required string Name { get; set; }
    public required string RequestorFirstName { get; set; }
    public required string RequestorLastName { get; set; }
    public Guid RequestId { get; set; }
    
    /// <summary>
    /// When true - it is a group. Otherwise, event.
    /// </summary>
    public bool IsGroup { get; set; }
}
