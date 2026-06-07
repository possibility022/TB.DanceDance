namespace Domain.Models;

public static class SasExpiry
{
    /// <summary>
    /// Rounds <paramref name="now"/> up to the next boundary of <paramref name="bucketSize"/>
    /// (measured in UTC ticks since the epoch), so repeated calls within the same bucket
    /// yield an identical instant — and therefore an identical SAS signature/URL.
    /// </summary>
    public static DateTimeOffset QuantizeToNextBoundary(DateTimeOffset now, TimeSpan bucketSize)
    {
        var ticks = now.UtcTicks;
        var bucketTicks = bucketSize.Ticks;
        var remainder = ticks % bucketTicks;
        var ceilingTicks = remainder == 0 ? ticks : ticks - remainder + bucketTicks;
        return new DateTimeOffset(ceilingTicks, TimeSpan.Zero);
    }
}
