namespace Domain.Entities;

/// <summary>
/// An owner-owned grouping of the owner's own private videos (e.g. all videos from a single
/// competition). A video belongs to at most one competition. Comments live at the competition
/// level so a teacher can give feedback on the whole competition in one combined thread.
/// </summary>
public class Competition
{
    public Guid Id { get; set; }

    /// <summary>
    /// Display name of the competition.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The user that owns this competition. Mirrors <see cref="Video.OwnerUserId"/>.
    /// </summary>
    public required string OwnerUserId { get; set; }

    /// <summary>
    /// Optional date of the competition, for display.
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Optional location of the competition, for display.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Controls who can see comments on this competition's combined thread.
    /// </summary>
    public CommentVisibility CommentVisibility { get; set; } = CommentVisibility.OwnerOnly;

    public DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// The videos grouped into this competition.
    /// </summary>
    public ICollection<Video> Videos { get; set; } = null!;
}
