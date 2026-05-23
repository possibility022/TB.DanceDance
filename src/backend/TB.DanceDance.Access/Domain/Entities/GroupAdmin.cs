namespace TB.DanceDance.Access.Domain.Entities;
public class GroupAdmin
{
    private GroupAdmin() { }
    
    public required Guid Id { get; set; }
    public required string UserId { get; set; }
    public required Guid GroupId { get; set; }

    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}
