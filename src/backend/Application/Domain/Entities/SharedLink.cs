namespace Domain.Entities;

public class SharedLink
{
    public string Id { get; set; } = null!;
    public Guid VideoId { get; set; }
    public string SharedBy { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpireAt { get; set; }
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// Whether commenting is allowed through this shared link.
    /// </summary>
    public bool AllowComments { get; set; } = true;

    /// <summary>
    /// Whether anonymous (non-logged-in) users can comment through this shared link.
    /// Only applies if AllowComments is true.
    /// </summary>
    public bool AllowAnonymousComments { get; set; } = false;

    public User SharedByUser { get; set; } = null!;
    public Video Video { get; set; } = null!;
}