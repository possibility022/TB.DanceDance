namespace Domain.Entities;

/// <summary>
/// A single video included in a <see cref="VideoTransfer"/>.
/// </summary>
public class VideoTransferItem
{
    public Guid Id { get; set; }

    public string TransferId { get; set; } = null!;
    public Guid VideoId { get; set; }

    public VideoTransfer Transfer { get; set; } = null!;
    public Video Video { get; set; } = null!;
}
