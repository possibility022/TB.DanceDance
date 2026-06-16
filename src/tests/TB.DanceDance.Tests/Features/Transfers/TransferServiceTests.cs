using Application.Features.AccessManagement;
using Application.Features.Transfers;
using Domain.Entities;
using Domain.Exceptions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Features.Transfers;

public class TransferServiceTests : BaseTestClass
{
    private ITransferService transferService = null!;

    public TransferServiceTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        var accessService = new AccessService(runtimeDbContext);
        transferService = new TransferService(runtimeDbContext, accessService);
        return ValueTask.CompletedTask;
    }

    // Builds a converted private video owned by the given user, with its private SharedWith row,
    // and stages both for persistence.
    private Video AddPrivateVideo(User owner, long convertedBlobSize = 0, string? name = null)
    {
        var builder = new VideoDataBuilder()
            .UploadedBy(owner)
            .Converted()
            .WithConvertedBlobSize(convertedBlobSize)
            .ShareAsPrivate(owner);
        if (name != null)
            builder.WithName(name);

        var video = builder.Build();
        var share = builder.BuildShares().Single();
        SeedDbContext.AddRange(video, share);
        return video;
    }

    private async Task<bool> IsPrivateVideoOf(string userId, Guid videoId)
    {
        SeedDbContext.ChangeTracker.Clear();
        return await SeedDbContext.SharedWith.AnyAsync(
            s => s.VideoId == videoId && s.UserId == userId && s.EventId == null && s.GroupId == null,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateTransfer_HappyPath_CreatesPendingTransferWithItems()
    {
        var sender = new UserDataBuilder().Build();
        var v1 = AddPrivateVideo(sender, name: "V1");
        var v2 = AddPrivateVideo(sender, name: "V2");
        SeedDbContext.Add(sender);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await transferService.CreateTransferAsync(
            sender.Id, new[] { v1.Id, v2.Id }, 7, TestContext.Current.CancellationToken);

        Assert.NotNull(transfer);
        Assert.Equal(8, transfer.Id.Length);
        Assert.Equal(sender.Id, transfer.CreatedBy);
        Assert.Equal(TransferStatus.Pending, transfer.Status);
        Assert.Equal(2, transfer.Items.Count);
        Assert.Equal(7, (transfer.ExpireAt - transfer.CreatedAt).Days);
    }

    [Fact]
    public async Task CreateTransfer_NotOwner_Throws()
    {
        var owner = new UserDataBuilder().Build();
        var other = new UserDataBuilder().Build();
        var video = AddPrivateVideo(owner);
        SeedDbContext.AddRange(owner, other);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            transferService.CreateTransferAsync(other.Id, new[] { video.Id }, 7, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateTransfer_NotConverted_Throws()
    {
        var sender = new UserDataBuilder().Build();
        var builder = new VideoDataBuilder().UploadedBy(sender).Converted(false).ShareAsPrivate(sender);
        var video = builder.Build();
        SeedDbContext.AddRange(sender, video, builder.BuildShares().Single());
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            transferService.CreateTransferAsync(sender.Id, new[] { video.Id }, 7, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateTransfer_NotPrivate_Throws()
    {
        var sender = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        var builder = new VideoDataBuilder().UploadedBy(sender).Converted().ShareWithGroup(group, sender);
        var video = builder.Build();
        SeedDbContext.AddRange(sender, group, video, builder.BuildShares().Single());
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            transferService.CreateTransferAsync(sender.Id, new[] { video.Id }, 7, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateTransfer_VideoAlreadyInPendingTransfer_Throws()
    {
        var sender = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender);
        SeedDbContext.Add(sender);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await transferService.CreateTransferAsync(sender.Id, new[] { video.Id }, 7, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            transferService.CreateTransferAsync(sender.Id, new[] { video.Id }, 7, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Accept_MovesOwnership_AllItems()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var v1 = AddPrivateVideo(sender, convertedBlobSize: 100);
        var v2 = AddPrivateVideo(sender, convertedBlobSize: 200);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await transferService.CreateTransferAsync(
            sender.Id, new[] { v1.Id, v2.Id }, 7, TestContext.Current.CancellationToken);

        var result = await transferService.AcceptTransferAsync(
            transfer.Id, recipient.Id, TestContext.Current.CancellationToken);

        Assert.Equal(AcceptTransferResult.Accepted, result);

        SeedDbContext.ChangeTracker.Clear();
        foreach (var id in new[] { v1.Id, v2.Id })
        {
            var video = await SeedDbContext.Videos.FirstAsync(v => v.Id == id, TestContext.Current.CancellationToken);
            Assert.Equal(recipient.Id, video.UploadedBy);
            Assert.True(await IsPrivateVideoOf(recipient.Id, id));
            Assert.False(await IsPrivateVideoOf(sender.Id, id));
        }

        var saved = await SeedDbContext.VideoTransfers.FirstAsync(t => t.Id == transfer.Id, TestContext.Current.CancellationToken);
        Assert.Equal(TransferStatus.Accepted, saved.Status);
        Assert.Equal(recipient.Id, saved.AcceptedByUserId);
        Assert.NotNull(saved.AcceptedAt);
    }

    [Fact]
    public async Task Accept_OverQuota_Blocked_OwnershipUnchanged()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        recipient.StorageQuotaBytes = 50; // smaller than the transfer size
        var video = AddPrivateVideo(sender, convertedBlobSize: 100);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await transferService.CreateTransferAsync(
            sender.Id, new[] { video.Id }, 7, TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<QuotaExceededException>(() =>
            transferService.AcceptTransferAsync(transfer.Id, recipient.Id, TestContext.Current.CancellationToken));
        Assert.Equal(100, ex.RequiredBytes);
        Assert.Equal(50, ex.AvailableBytes);

        // Ownership unchanged
        SeedDbContext.ChangeTracker.Clear();
        var reloaded = await SeedDbContext.Videos.FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal(sender.Id, reloaded.UploadedBy);
        Assert.True(await IsPrivateVideoOf(sender.Id, video.Id));
        var savedTransfer = await SeedDbContext.VideoTransfers.FirstAsync(t => t.Id == transfer.Id, TestContext.Current.CancellationToken);
        Assert.Equal(TransferStatus.Pending, savedTransfer.Status);
    }

    [Fact]
    public async Task Accept_RevokesSendersActiveShareLinks()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        var shareLink = new SharedLinkDataBuilder().WithId("shlink01").ForVideo(video).SharedBy(sender).ExpiresInDays(30).Build();
        SeedDbContext.AddRange(sender, recipient, shareLink);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await transferService.CreateTransferAsync(
            sender.Id, new[] { video.Id }, 7, TestContext.Current.CancellationToken);
        await transferService.AcceptTransferAsync(transfer.Id, recipient.Id, TestContext.Current.CancellationToken);

        SeedDbContext.ChangeTracker.Clear();
        var link = await SeedDbContext.SharedLinks.FirstAsync(l => l.Id == "shlink01", TestContext.Current.CancellationToken);
        Assert.True(link.IsRevoked);
    }

    [Fact]
    public async Task Accept_OwnTransfer_ReturnsCannotAcceptOwnTransfer()
    {
        var sender = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        SeedDbContext.Add(sender);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await transferService.CreateTransferAsync(
            sender.Id, new[] { video.Id }, 7, TestContext.Current.CancellationToken);

        var result = await transferService.AcceptTransferAsync(
            transfer.Id, sender.Id, TestContext.Current.CancellationToken);

        Assert.Equal(AcceptTransferResult.CannotAcceptOwnTransfer, result);
    }

    [Fact]
    public async Task Accept_Twice_SecondReturnsNotAvailable()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await transferService.CreateTransferAsync(
            sender.Id, new[] { video.Id }, 7, TestContext.Current.CancellationToken);

        var first = await transferService.AcceptTransferAsync(transfer.Id, recipient.Id, TestContext.Current.CancellationToken);
        var second = await transferService.AcceptTransferAsync(transfer.Id, recipient.Id, TestContext.Current.CancellationToken);

        Assert.Equal(AcceptTransferResult.Accepted, first);
        Assert.Equal(AcceptTransferResult.NotAvailable, second);
    }

    [Fact]
    public async Task Accept_AfterRevoke_ReturnsNotAvailable()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await transferService.CreateTransferAsync(
            sender.Id, new[] { video.Id }, 7, TestContext.Current.CancellationToken);
        await transferService.RevokeTransferAsync(transfer.Id, sender.Id, TestContext.Current.CancellationToken);

        var result = await transferService.AcceptTransferAsync(transfer.Id, recipient.Id, TestContext.Current.CancellationToken);
        Assert.Equal(AcceptTransferResult.NotAvailable, result);
    }

    [Fact]
    public async Task Decline_Pending_SetsDeclined_AndBlocksSender()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await transferService.CreateTransferAsync(
            sender.Id, new[] { video.Id }, 7, TestContext.Current.CancellationToken);

        // sender cannot decline
        Assert.False(await transferService.DeclineTransferAsync(transfer.Id, sender.Id, TestContext.Current.CancellationToken));
        // recipient can decline
        Assert.True(await transferService.DeclineTransferAsync(transfer.Id, recipient.Id, TestContext.Current.CancellationToken));

        Assert.Null(await transferService.GetTransferAsync(transfer.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Revoke_OnlySender()
    {
        var sender = new UserDataBuilder().Build();
        var other = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        SeedDbContext.AddRange(sender, other);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await transferService.CreateTransferAsync(
            sender.Id, new[] { video.Id }, 7, TestContext.Current.CancellationToken);

        Assert.False(await transferService.RevokeTransferAsync(transfer.Id, other.Id, TestContext.Current.CancellationToken));
        Assert.True(await transferService.RevokeTransferAsync(transfer.Id, sender.Id, TestContext.Current.CancellationToken));
        Assert.Null(await transferService.GetTransferAsync(transfer.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetTransfer_Expired_ReturnsNull()
    {
        var sender = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        var transfer = new VideoTransferDataBuilder()
            .WithId("expired1")
            .CreatedBy(sender)
            .CreatedAt(DateTimeOffset.UtcNow.AddDays(-10))
            .ExpiresAt(DateTimeOffset.UtcNow.AddDays(-1))
            .WithVideo(video)
            .Build();
        SeedDbContext.AddRange(sender);
        SeedDbContext.Add(transfer);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Null(await transferService.GetTransferAsync("expired1", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ListMyOutgoing_NewestFirst()
    {
        var sender = new UserDataBuilder().Build();
        var va = AddPrivateVideo(sender, convertedBlobSize: 10);
        var vb = AddPrivateVideo(sender, convertedBlobSize: 10);
        var older = new VideoTransferDataBuilder().WithId("older001").CreatedBy(sender)
            .CreatedAt(DateTimeOffset.UtcNow.AddDays(-2)).ExpiresAt(DateTimeOffset.UtcNow.AddDays(5)).WithVideo(va).Build();
        var newer = new VideoTransferDataBuilder().WithId("newer001").CreatedBy(sender)
            .CreatedAt(DateTimeOffset.UtcNow.AddDays(-1)).ExpiresAt(DateTimeOffset.UtcNow.AddDays(6)).WithVideo(vb).Build();
        SeedDbContext.Add(sender);
        SeedDbContext.AddRange(older, newer);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = (await transferService.ListMyOutgoingTransfersAsync(sender.Id, TestContext.Current.CancellationToken)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("newer001", result[0].Id);
        Assert.Equal("older001", result[1].Id);
    }
}
