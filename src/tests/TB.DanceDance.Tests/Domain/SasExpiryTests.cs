using Domain.Models;

namespace TB.DanceDance.Tests.Domain;

public class SasExpiryTests
{
    private static readonly TimeSpan ThirtyMinutes = TimeSpan.FromMinutes(30);

    [Fact]
    public void QuantizeToNextBoundary_RoundsUpToNextBucket_FromMidBucketInstant()
    {
        var now = new DateTimeOffset(2026, 6, 7, 10, 12, 34, TimeSpan.Zero);
        var expected = new DateTimeOffset(2026, 6, 7, 10, 30, 0, TimeSpan.Zero);

        Assert.Equal(expected, SasExpiry.QuantizeToNextBoundary(now, ThirtyMinutes));
    }

    [Fact]
    public void QuantizeToNextBoundary_RoundsUpToNextBucket_FromJustBeforeBoundary()
    {
        var now = new DateTimeOffset(2026, 6, 7, 10, 29, 59, TimeSpan.Zero);
        var expected = new DateTimeOffset(2026, 6, 7, 10, 30, 0, TimeSpan.Zero);

        Assert.Equal(expected, SasExpiry.QuantizeToNextBoundary(now, ThirtyMinutes));
    }

    [Fact]
    public void QuantizeToNextBoundary_StaysPut_WhenAlreadyOnBoundary()
    {
        var now = new DateTimeOffset(2026, 6, 7, 10, 30, 0, TimeSpan.Zero);

        Assert.Equal(now, SasExpiry.QuantizeToNextBoundary(now, ThirtyMinutes));
    }

    [Fact]
    public void QuantizeToNextBoundary_RoundsUpToFollowingBucket_FromJustAfterBoundary()
    {
        var now = new DateTimeOffset(2026, 6, 7, 10, 30, 1, TimeSpan.Zero);
        var expected = new DateTimeOffset(2026, 6, 7, 11, 0, 0, TimeSpan.Zero);

        Assert.Equal(expected, SasExpiry.QuantizeToNextBoundary(now, ThirtyMinutes));
    }

    [Fact]
    public void QuantizeToNextBoundary_ReturnsResultWithUtcOffset()
    {
        var now = new DateTimeOffset(2026, 6, 7, 10, 0, 0, TimeSpan.Zero);

        Assert.Equal(TimeSpan.Zero, SasExpiry.QuantizeToNextBoundary(now, ThirtyMinutes).Offset);
    }
}
