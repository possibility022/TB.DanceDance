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
    private ITransferService seedingTransferService = null!;

    public TransferServiceTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        var accessService = new AccessService(runtimeDbContext);
        transferService = new TransferService(runtimeDbContext, accessService);
        seedingTransferService = new TransferService(SeedDbContext, new AccessService(SeedDbContext));
        return ValueTask.CompletedTask;
    }

    // Builds a converted private video owned by the given user, with its private SharedWith row,
    // and stages both for persistence.
    private Video AddPrivateVideo(User owner, long convertedBlobSize = 0, string? name = null)
    {
        var builder = new VideoDataBuilder()
            .OwnedBy(owner)
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
    public async Task CreateTransfer_HappyPath_CreatesPendingTransferWithItem()
    {
        var sender = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, name: "V1");
        SeedDbContext.Add(sender);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await transferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);

        Assert.NotNull(transfer);
        Assert.Equal(8, transfer.Id.Length);
        Assert.Equal(sender.Id, transfer.CreatedBy);
        Assert.Equal(TransferStatus.Pending, transfer.Status);
        Assert.Single(transfer.Items);
        Assert.Equal(video.Id, transfer.Items.Single().VideoId);
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
            transferService.CreateTransferAsync(other.Id, video.Id, 7, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateTransfer_NotConverted_Throws()
    {
        var sender = new UserDataBuilder().Build();
        var builder = new VideoDataBuilder().OwnedBy(sender).Converted(false).ShareAsPrivate(sender);
        var video = builder.Build();
        SeedDbContext.AddRange(sender, video, builder.BuildShares().Single());
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            transferService.CreateTransferAsync(sender.Id, video.Id, 7, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateTransfer_NotPrivate_Throws()
    {
        var sender = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        var builder = new VideoDataBuilder().OwnedBy(sender).Converted().ShareWithGroup(group, sender);
        var video = builder.Build();
        SeedDbContext.AddRange(sender, group, video, builder.BuildShares().Single());
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            transferService.CreateTransferAsync(sender.Id, video.Id, 7, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateTransfer_VideoAlreadyInPendingTransfer_Throws()
    {
        var sender = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender);
        SeedDbContext.Add(sender);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await seedingTransferService.CreateTransferAsync(sender.Id, video.Id, 7, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            transferService.CreateTransferAsync(sender.Id, video.Id, 7, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Accept_ParksInAccepted_OwnershipUnchanged()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 100);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);

        var result = await transferService.AcceptTransferAsync(
            transfer.Id, recipient.Id, TestContext.Current.CancellationToken);

        Assert.Equal(AcceptTransferResult.Accepted, result);

        SeedDbContext.ChangeTracker.Clear();
        // Ownership must NOT have moved yet.
        var unchanged = await SeedDbContext.Videos.FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal(sender.Id, unchanged.OwnerUserId);
        Assert.True(await IsPrivateVideoOf(sender.Id, video.Id));
        Assert.False(await IsPrivateVideoOf(recipient.Id, video.Id));

        var saved = await SeedDbContext.VideoTransfers.FirstAsync(t => t.Id == transfer.Id, TestContext.Current.CancellationToken);
        Assert.Equal(TransferStatus.Accepted, saved.Status);
        Assert.Equal(recipient.Id, saved.AcceptedByUserId);
        Assert.NotNull(saved.AcceptedAt);
        Assert.Null(saved.ApprovedAt);
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

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<QuotaExceededException>(() =>
            transferService.AcceptTransferAsync(transfer.Id, recipient.Id, TestContext.Current.CancellationToken));
        Assert.Equal(100, ex.RequiredBytes);
        Assert.Equal(50, ex.AvailableBytes);

        SeedDbContext.ChangeTracker.Clear();
        var reloaded = await SeedDbContext.Videos.FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal(sender.Id, reloaded.OwnerUserId);
        Assert.True(await IsPrivateVideoOf(sender.Id, video.Id));
        var savedTransfer = await SeedDbContext.VideoTransfers.FirstAsync(t => t.Id == transfer.Id, TestContext.Current.CancellationToken);
        Assert.Equal(TransferStatus.Pending, savedTransfer.Status);
    }

    [Fact]
    public async Task Accept_DoesNotRevokeShareLinks()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        var shareLink = new SharedLinkDataBuilder().WithId("shlink01").ForVideo(video).SharedBy(sender).ExpiresInDays(30).Build();
        SeedDbContext.AddRange(sender, recipient, shareLink);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);
        await transferService.AcceptTransferAsync(transfer.Id, recipient.Id, TestContext.Current.CancellationToken);

        SeedDbContext.ChangeTracker.Clear();
        var link = await SeedDbContext.SharedLinks.FirstAsync(l => l.Id == "shlink01", TestContext.Current.CancellationToken);
        Assert.False(link.IsRevoked);
    }

    [Fact]
    public async Task Accept_OwnTransfer_ReturnsCannotAcceptOwnTransfer()
    {
        var sender = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        SeedDbContext.Add(sender);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);

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

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);

        var first = await seedingTransferService.AcceptTransferAsync(transfer.Id, recipient.Id, TestContext.Current.CancellationToken);
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

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);
        await seedingTransferService.RevokeTransferAsync(transfer.Id, sender.Id, TestContext.Current.CancellationToken);

        var result = await transferService.AcceptTransferAsync(transfer.Id, recipient.Id, TestContext.Current.CancellationToken);
        Assert.Equal(AcceptTransferResult.NotAvailable, result);
    }

    [Fact]
    public async Task Approve_HappyPath_MovesOwnership()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        var shareLink = new SharedLinkDataBuilder().WithId("shlinkApprove").ForVideo(video).SharedBy(sender).ExpiresInDays(30).Build();
        SeedDbContext.AddRange(sender, recipient, shareLink);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);
        await seedingTransferService.AcceptTransferAsync(
            transfer.Id, recipient.Id, TestContext.Current.CancellationToken);

        var result = await transferService.ApproveTransferAsync(
            transfer.Id, sender.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ApproveTransferResult.Approved, result);

        SeedDbContext.ChangeTracker.Clear();
        var moved = await SeedDbContext.Videos.FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal(recipient.Id, moved.OwnerUserId);
        Assert.True(await IsPrivateVideoOf(recipient.Id, video.Id));
        Assert.False(await IsPrivateVideoOf(sender.Id, video.Id));

        var link = await SeedDbContext.SharedLinks.FirstAsync(l => l.Id == "shlinkApprove", TestContext.Current.CancellationToken);
        Assert.True(link.IsRevoked);

        var saved = await SeedDbContext.VideoTransfers.FirstAsync(t => t.Id == transfer.Id, TestContext.Current.CancellationToken);
        Assert.Equal(TransferStatus.Approved, saved.Status);
        Assert.NotNull(saved.ApprovedAt);
    }

    [Fact]
    public async Task Approve_ByNonOwner_ReturnsNotOwner()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var other = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        SeedDbContext.AddRange(sender, recipient, other);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);
        await seedingTransferService.AcceptTransferAsync(
            transfer.Id, recipient.Id, TestContext.Current.CancellationToken);

        var result = await transferService.ApproveTransferAsync(
            transfer.Id, other.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ApproveTransferResult.NotOwner, result);

        SeedDbContext.ChangeTracker.Clear();
        var unchanged = await SeedDbContext.Videos.FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal(sender.Id, unchanged.OwnerUserId);
    }

    [Fact]
    public async Task Approve_WhenNotAccepted_ReturnsNotAvailable()
    {
        var sender = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        SeedDbContext.Add(sender);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);

        // Transfer is still Pending — not accepted yet
        var result = await transferService.ApproveTransferAsync(
            transfer.Id, sender.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ApproveTransferResult.NotAvailable, result);
    }

    [Fact]
    public async Task Approve_OverQuota_Throws_OwnershipUnchanged()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        recipient.StorageQuotaBytes = 1000; // enough to accept but will be tightened before approve
        var video = AddPrivateVideo(sender, convertedBlobSize: 100);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);
        await seedingTransferService.AcceptTransferAsync(
            transfer.Id, recipient.Id, TestContext.Current.CancellationToken);

        // Shrink the recipient's quota below the transfer size before the owner approves
        SeedDbContext.ChangeTracker.Clear();
        var dbRecipient = await SeedDbContext.Users.FirstAsync(u => u.Id == recipient.Id, TestContext.Current.CancellationToken);
        dbRecipient.StorageQuotaBytes = 50;
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<QuotaExceededException>(() =>
            transferService.ApproveTransferAsync(transfer.Id, sender.Id, TestContext.Current.CancellationToken));

        Assert.Equal(100, ex.RequiredBytes);

        SeedDbContext.ChangeTracker.Clear();
        var unchanged = await SeedDbContext.Videos.FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal(sender.Id, unchanged.OwnerUserId);
        var savedTransfer = await SeedDbContext.VideoTransfers.FirstAsync(t => t.Id == transfer.Id, TestContext.Current.CancellationToken);
        Assert.Equal(TransferStatus.Accepted, savedTransfer.Status);
    }

    [Fact]
    public async Task Cancel_FromAccepted_SetsStatusCancelled_OwnershipUnchanged()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);
        await seedingTransferService.AcceptTransferAsync(
            transfer.Id, recipient.Id, TestContext.Current.CancellationToken);

        var cancelled = await transferService.CancelTransferAsync(
            transfer.Id, sender.Id, TestContext.Current.CancellationToken);

        Assert.True(cancelled);

        SeedDbContext.ChangeTracker.Clear();
        var saved = await SeedDbContext.VideoTransfers.FirstAsync(t => t.Id == transfer.Id, TestContext.Current.CancellationToken);
        Assert.Equal(TransferStatus.Cancelled, saved.Status);
        var unchanged = await SeedDbContext.Videos.FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal(sender.Id, unchanged.OwnerUserId);
    }

    [Fact]
    public async Task Cancel_ByNonOwner_ReturnsFalse()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);
        await seedingTransferService.AcceptTransferAsync(
            transfer.Id, recipient.Id, TestContext.Current.CancellationToken);

        var result = await transferService.CancelTransferAsync(
            transfer.Id, recipient.Id, TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task GetTransfer_Accepted_IsLive()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);
        await seedingTransferService.AcceptTransferAsync(
            transfer.Id, recipient.Id, TestContext.Current.CancellationToken);

        var found = await transferService.GetTransferAsync(transfer.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(found);
        Assert.Equal(TransferStatus.Accepted, found.Status);
    }

    [Fact]
    public async Task GetTransfer_Approved_IsLive()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);
        await seedingTransferService.AcceptTransferAsync(
            transfer.Id, recipient.Id, TestContext.Current.CancellationToken);
        await transferService.ApproveTransferAsync(
            transfer.Id, sender.Id, TestContext.Current.CancellationToken);

        var found = await transferService.GetTransferAsync(transfer.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(found);
        Assert.Equal(TransferStatus.Approved, found.Status);
    }

    [Fact]
    public async Task GetTransfer_Cancelled_ReturnsNull()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);
        await seedingTransferService.AcceptTransferAsync(
            transfer.Id, recipient.Id, TestContext.Current.CancellationToken);
        await seedingTransferService.CancelTransferAsync(
            transfer.Id, sender.Id, TestContext.Current.CancellationToken);

        Assert.Null(await transferService.GetTransferAsync(transfer.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Decline_Pending_SetsDeclined_AndBlocksSender()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, convertedBlobSize: 10);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);

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

        var transfer = await seedingTransferService.CreateTransferAsync(
            sender.Id, video.Id, 7, TestContext.Current.CancellationToken);

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
