using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Features.Transfers;

public class VideoTransferPersistenceTests : BaseTestClass
{
    public VideoTransferPersistenceTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext) => ValueTask.CompletedTask;

    [Fact]
    public async Task VideoTransfer_WithItems_RoundTrips()
    {
        // Arrange
        var sender = new UserDataBuilder().Build();
        var video1 = new VideoDataBuilder().UploadedBy(sender).WithName("Video 1").Build();
        var video2 = new VideoDataBuilder().UploadedBy(sender).WithName("Video 2").Build();
        var transfer = new VideoTransferDataBuilder()
            .WithId("trnsf001")
            .CreatedBy(sender)
            .ExpiresInDays(7)
            .WithVideo(video1)
            .WithVideo(video2)
            .Build();

        SeedDbContext.AddRange(sender, video1, video2);
        SeedDbContext.Add(transfer);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        // Act
        var reloaded = await SeedDbContext.Set<VideoTransfer>()
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == "trnsf001", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(reloaded);
        Assert.Equal(sender.Id, reloaded!.CreatedBy);
        Assert.Equal(TransferStatus.Pending, reloaded.Status);
        Assert.Null(reloaded.AcceptedByUserId);
        Assert.Null(reloaded.AcceptedAt);
        Assert.True(reloaded.ExpireAt > reloaded.CreatedAt);
        Assert.Equal(2, reloaded.Items.Count);
        Assert.Contains(reloaded.Items, i => i.VideoId == video1.Id);
        Assert.Contains(reloaded.Items, i => i.VideoId == video2.Id);
    }

    [Fact]
    public async Task VideoTransfer_AcceptedFields_RoundTrip()
    {
        // Arrange
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(sender).Build();
        var acceptedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var transfer = new VideoTransferDataBuilder()
            .WithId("trnsf002")
            .CreatedBy(sender)
            .WithVideo(video)
            .AcceptedBy(recipient, acceptedAt)
            .Build();

        SeedDbContext.AddRange(sender, recipient, video);
        SeedDbContext.Add(transfer);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        // Act
        var reloaded = await SeedDbContext.Set<VideoTransfer>()
            .FirstOrDefaultAsync(t => t.Id == "trnsf002", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(reloaded);
        Assert.Equal(TransferStatus.Accepted, reloaded!.Status);
        Assert.Equal(recipient.Id, reloaded.AcceptedByUserId);
        Assert.NotNull(reloaded.AcceptedAt);
    }

    [Fact]
    public async Task DeletingVideo_DropsItsPendingTransferItem()
    {
        // Arrange
        var sender = new UserDataBuilder().Build();
        var keep = new VideoDataBuilder().UploadedBy(sender).WithName("Keep").Build();
        var toDelete = new VideoDataBuilder().UploadedBy(sender).WithName("Delete").Build();
        var transfer = new VideoTransferDataBuilder()
            .WithId("trnsf003")
            .CreatedBy(sender)
            .WithVideo(keep)
            .WithVideo(toDelete)
            .Build();

        SeedDbContext.AddRange(sender, keep, toDelete);
        SeedDbContext.Add(transfer);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        // Act - delete one of the videos
        var video = await SeedDbContext.Videos.FirstAsync(v => v.Id == toDelete.Id, TestContext.Current.CancellationToken);
        SeedDbContext.Videos.Remove(video);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        // Assert - the item for the deleted video is gone (FK cascade), the other remains, the transfer survives
        var items = await SeedDbContext.Set<VideoTransferItem>()
            .Where(i => i.TransferId == "trnsf003")
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(items);
        Assert.Equal(keep.Id, items[0].VideoId);

        var transferStillExists = await SeedDbContext.Set<VideoTransfer>()
            .AnyAsync(t => t.Id == "trnsf003", TestContext.Current.CancellationToken);
        Assert.True(transferStillExists);
    }
}
