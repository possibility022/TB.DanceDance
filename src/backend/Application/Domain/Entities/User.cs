namespace Domain.Entities;
public class User
{
    public required string Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }

    /// <summary>
    /// Storage quota for private videos in bytes. Default 1GB.
    /// Only applies to converted video size (ConvertedBlobSize).
    /// </summary>
    public long StorageQuotaBytes { get; set; } = 1073741824; // 1GB default
}
