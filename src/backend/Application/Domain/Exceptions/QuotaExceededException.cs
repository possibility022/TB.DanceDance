namespace Domain.Exceptions;

/// <summary>
/// Thrown when an operation would push a user's private-video storage above their quota.
/// Carries the numbers so the API can return a clear message (required vs available bytes).
/// </summary>
public class QuotaExceededException : AppException
{
    public long RequiredBytes { get; }
    public long AvailableBytes { get; }

    public QuotaExceededException(long requiredBytes, long availableBytes)
        : base($"Accepting this transfer requires {requiredBytes} bytes but only {availableBytes} bytes are available.")
    {
        RequiredBytes = requiredBytes;
        AvailableBytes = availableBytes;
    }
}
