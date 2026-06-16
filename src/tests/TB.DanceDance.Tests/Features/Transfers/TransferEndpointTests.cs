using System.Security.Claims;
using Application;
using Application.Features.AccessManagement;
using Application.Features.Transfers;
using Application.Features.Transfers.Endpoints;
using Application.Features.Videos;
using Domain.Entities;
using FastEndpoints;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using TB.DanceDance.API.Contracts.Features.Transfers;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Features.Transfers;

/// <summary>
/// Endpoint-level tests driving each endpoint's HandleAsync via FastEndpoints' Factory.Create with a
/// real TransferService over the Testcontainers DB. The auth/scope enforcement itself is declarative
/// (every endpoint uses Policies(ApiScopes.Read); get-info deliberately does NOT AllowAnonymous,
/// unlike the shared-link info endpoint) and lives in the middleware pipeline, so these tests focus
/// on the per-endpoint status-code mapping and behavior.
/// </summary>
public class TransferEndpointTests : BaseTestClass
{
    private ITransferService transferService = null!;
    private readonly IOptions<AppOptions> appOptions = Options.Create(new AppOptions { AppWebsiteOrigin = "https://app.test" });

    public TransferEndpointTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        transferService = new TransferService(runtimeDbContext, new AccessService(runtimeDbContext));
        return ValueTask.CompletedTask;
    }

    private static DefaultHttpContext Ctx(string? sub, params (string Key, object Value)[] routeValues)
    {
        var ctx = new DefaultHttpContext();
        if (sub != null)
        {
            var identity = new ClaimsIdentity(new[] { new Claim("sub", sub) }, "test");
            ctx.User = new ClaimsPrincipal(identity);
        }
        foreach (var (key, value) in routeValues)
            ctx.Request.RouteValues[key] = value;
        return ctx;
    }

    private Video AddPrivateVideo(User owner, long convertedBlobSize = 0)
    {
        var builder = new VideoDataBuilder()
            .UploadedBy(owner)
            .Converted()
            .WithConvertedBlobSize(convertedBlobSize)
            .WithBlobId($"blob-{Guid.NewGuid():N}")
            .ShareAsPrivate(owner);
        var video = builder.Build();
        SeedDbContext.AddRange(video, builder.BuildShares().Single());
        return video;
    }

    private async Task<VideoTransfer> CreatePendingTransfer(User sender, params Video[] videos)
        => await transferService.CreateTransferAsync(
            sender.Id, videos.Select(v => v.Id).ToArray(), 7, TestContext.Current.CancellationToken);

    [Fact]
    public async Task GetInfo_RevokedTransfer_Returns404()
    {
        var sender = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, 10);
        SeedDbContext.Add(sender);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var transfer = await CreatePendingTransfer(sender, video);
        await transferService.RevokeTransferAsync(transfer.Id, sender.Id, TestContext.Current.CancellationToken);

        var ep = Factory.Create<GetTransferInfoEndpoint>(Ctx("recipient", ("linkId", transfer.Id)), transferService);
        await ep.HandleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(404, ep.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task GetInfo_PendingTransfer_Returns200WithItemsAndTotal()
    {
        var sender = new UserDataBuilder().Build();
        var v1 = AddPrivateVideo(sender, 30);
        var v2 = AddPrivateVideo(sender, 70);
        SeedDbContext.Add(sender);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var transfer = await CreatePendingTransfer(sender, v1, v2);

        var ep = Factory.Create<GetTransferInfoEndpoint>(Ctx("recipient", ("linkId", transfer.Id)), transferService);
        await ep.HandleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(200, ep.HttpContext.Response.StatusCode);
        Assert.NotNull(ep.Response);
        Assert.Equal("Pending", ep.Response.Status);
        Assert.Equal(2, ep.Response.Items.Count);
        Assert.Equal(100, ep.Response.TotalSizeBytes);
    }

    [Fact]
    public async Task Accept_OverQuota_Returns409WithBytes()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        recipient.StorageQuotaBytes = 50;
        var video = AddPrivateVideo(sender, 100);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var transfer = await CreatePendingTransfer(sender, video);

        var ep = Factory.Create<AcceptTransferEndpoint>(Ctx(recipient.Id, ("linkId", transfer.Id)), transferService);
        await ep.HandleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(409, ep.HttpContext.Response.StatusCode);
        Assert.False(ep.Response.Accepted);
        Assert.Equal(100, ep.Response.RequiredBytes);
        Assert.Equal(50, ep.Response.AvailableBytes);
    }

    [Fact]
    public async Task Accept_OwnTransfer_Returns400()
    {
        var sender = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, 10);
        SeedDbContext.Add(sender);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var transfer = await CreatePendingTransfer(sender, video);

        var ep = Factory.Create<AcceptTransferEndpoint>(Ctx(sender.Id, ("linkId", transfer.Id)), transferService);
        await ep.HandleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(400, ep.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task Accept_RevokedTransfer_Returns404()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, 10);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var transfer = await CreatePendingTransfer(sender, video);
        await transferService.RevokeTransferAsync(transfer.Id, sender.Id, TestContext.Current.CancellationToken);

        var ep = Factory.Create<AcceptTransferEndpoint>(Ctx(recipient.Id, ("linkId", transfer.Id)), transferService);
        await ep.HandleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(404, ep.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task Accept_Valid_Returns200Accepted()
    {
        var sender = new UserDataBuilder().Build();
        var recipient = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, 10);
        SeedDbContext.AddRange(sender, recipient);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var transfer = await CreatePendingTransfer(sender, video);

        var ep = Factory.Create<AcceptTransferEndpoint>(Ctx(recipient.Id, ("linkId", transfer.Id)), transferService);
        await ep.HandleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(200, ep.HttpContext.Response.StatusCode);
        Assert.True(ep.Response.Accepted);
    }

    [Fact]
    public async Task Stream_AsSender_Returns404()
    {
        var sender = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, 10);
        SeedDbContext.Add(sender);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var transfer = await CreatePendingTransfer(sender, video);

        var videoService = Substitute.For<IVideoService>();
        var ep = Factory.Create<StreamVideoByTransferEndpoint>(
            Ctx(sender.Id, ("linkId", transfer.Id), ("videoId", video.Id)), transferService, videoService);
        await ep.HandleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(404, ep.HttpContext.Response.StatusCode);
        await videoService.DidNotReceiveWithAnyArgs().OpenStream(default!, default);
    }

    [Fact]
    public async Task Stream_RevokedTransfer_Returns404()
    {
        var sender = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, 10);
        SeedDbContext.Add(sender);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var transfer = await CreatePendingTransfer(sender, video);
        await transferService.RevokeTransferAsync(transfer.Id, sender.Id, TestContext.Current.CancellationToken);

        var videoService = Substitute.For<IVideoService>();
        var ep = Factory.Create<StreamVideoByTransferEndpoint>(
            Ctx("recipient", ("linkId", transfer.Id), ("videoId", video.Id)), transferService, videoService);
        await ep.HandleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(404, ep.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task Revoke_AsNonSender_Returns404()
    {
        var sender = new UserDataBuilder().Build();
        var other = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, 10);
        SeedDbContext.AddRange(sender, other);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var transfer = await CreatePendingTransfer(sender, video);

        var ep = Factory.Create<RevokeTransferEndpoint>(Ctx(other.Id, ("linkId", transfer.Id)), transferService);
        await ep.HandleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(404, ep.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task Revoke_AsSender_Returns204()
    {
        var sender = new UserDataBuilder().Build();
        var video = AddPrivateVideo(sender, 10);
        SeedDbContext.Add(sender);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var transfer = await CreatePendingTransfer(sender, video);

        var ep = Factory.Create<RevokeTransferEndpoint>(Ctx(sender.Id, ("linkId", transfer.Id)), transferService);
        await ep.HandleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(204, ep.HttpContext.Response.StatusCode);
    }
}
