using Azure;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Threading.Channels;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models.Storage;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.Library.Services.Network;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class UploadWorkerTests
{
    private static VideosDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<VideosDbContext>()
            .UseInMemoryDatabase(databaseName: $"vids-{Guid.NewGuid()}")
            .Options;
        return new VideosDbContext(options);
    }

    private static (UploadWorker worker,
        VideosDbContext db,
        IVideoUploader uploader,
        IDanceHttpApiClient api,
        IPlatformNotification platform,
        Channel<UploadProgressEvent> channel) CreateSut()
    {
        var db = CreateDb();
        var uploader = Substitute.For<IVideoUploader>();
        var api = Substitute.For<IDanceHttpApiClient>();
        var platform = Substitute.For<IPlatformNotification>();
        var channel = Channel.CreateUnbounded<UploadProgressEvent>();
        var worker = new UploadWorker(db, uploader, api, channel);
        worker.SetPlatformNotification(platform);
        return (worker, db, uploader, api, platform, channel);
    }

    [Fact]
    public async Task Work_NoVideos_Completes_And_Notifies()
    {
        var (worker, db, uploader, api, platform, channel) = CreateSut();

        await worker.Work(CancellationToken.None);

        await uploader.DidNotReceiveWithAnyArgs().Upload(null!, TestContext.Current.CancellationToken);
        platform.Received(1).UploadCompleteNotification();
    }

    [Fact]
    public async Task Work_UploadsAllPending_Videos_MarkUploaded_And_Saves()
    {
        var (worker, db, uploader, api, platform, channel) = CreateSut();

        // Arrange two pending videos with valid SAS
        var v1 = new VideosToUpload
        {
            Id = Guid.NewGuid(),
            FileName = "a.mp4",
            FullFileName = Path.GetTempFileName(),
            Uploaded = false,
            RemoteVideoId = Guid.NewGuid(),
            Sas = "https://example/sas1",
            SasExpireAt = DateTime.UtcNow.AddHours(1)
        };
        var v2 = new VideosToUpload
        {
            Id = Guid.NewGuid(),
            FileName = "b.mp4",
            FullFileName = Path.GetTempFileName(),
            Uploaded = false,
            RemoteVideoId = Guid.NewGuid(),
            Sas = "https://example/sas2",
            SasExpireAt = DateTime.UtcNow.AddHours(2)
        };
        db.VideosToUpload.AddRange(v1, v2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await worker.Work(CancellationToken.None);

        await uploader.Received(2).Upload(Arg.Any<VideosToUpload>(), Arg.Any<CancellationToken>());
        var rows = await db.VideosToUpload.AsNoTracking()
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.All(rows, r => Assert.True(r.Uploaded));
        platform.Received(1).UploadCompleteNotification();

        // cleanup temp files
        File.Delete(v1.FullFileName);
        File.Delete(v2.FullFileName);
    }

    [Fact]
    public async Task Work_ExpiredSas_Refreshes_And_UpdatesValues()
    {
        var (worker, db, uploader, api, platform, channel) = CreateSut();
        var id = Guid.NewGuid();
        var v = new VideosToUpload
        {
            Id = Guid.NewGuid(),
            FileName = "c.mp4",
            FullFileName = Path.GetTempFileName(),
            Uploaded = false,
            RemoteVideoId = id,
            Sas = "old-sas",
            SasExpireAt = DateTime.UtcNow.AddHours(-2) // expired
        };
        db.VideosToUpload.Add(v);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var refreshed = new UploadVideoInformationResponse
        {
            VideoId = id, Sas = "https://example/new-sas", ExpireAt = DateTimeOffset.UtcNow.AddHours(3)
        };
        api.RefreshUploadUrl(id).Returns(Task.FromResult(refreshed));

        await worker.Work(CancellationToken.None);

        await api.Received(1).RefreshUploadUrl(id);
        var updated = await db.VideosToUpload.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("https://example/new-sas", updated.Sas);
        Assert.True(updated.SasExpireAt > DateTime.UtcNow);
        Assert.True(updated.Uploaded);

        File.Delete(v.FullFileName);
    }

    [Fact]
    public async Task Work_On403_RefreshesSas_Then_UploadsSuccessfully()
    {
        var (worker, db, uploader, api, platform, channel) = CreateSut();
        var id = Guid.NewGuid();
        var v = new VideosToUpload
        {
            Id = Guid.NewGuid(),
            FileName = "d.mp4",
            FullFileName = Path.GetTempFileName(),
            Uploaded = false,
            RemoteVideoId = id,
            Sas = "sas1",
            SasExpireAt = DateTime.UtcNow.AddHours(2)
        };
        db.VideosToUpload.Add(v);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // First call throws 403, then success
        uploader.Upload(Arg.Any<VideosToUpload>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromException(new RequestFailedException(403, "Forbidden")),
                ci => Task.CompletedTask);

        var refreshed = new UploadVideoInformationResponse
        {
            VideoId = id, Sas = "sas2", ExpireAt = DateTimeOffset.UtcNow.AddHours(3)
        };
        api.RefreshUploadUrl(id).Returns(Task.FromResult(refreshed));

        await worker.Work(CancellationToken.None);

        await api.Received(1).RefreshUploadUrl(id);
        var row = await db.VideosToUpload.FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(row.Uploaded);

        File.Delete(v.FullFileName);
    }

    [Fact]
    public async Task MonitorProgress_ForwardsToPlatformNotification()
    {
        var (worker, db, uploader, api, platform, channel) = CreateSut();
        // Preload a message into the channel before work starts
        await channel.Writer.WriteAsync(
            new UploadProgressEvent { FileName = "vid.mp4", FileSize = 100, SendBytes = 50 },
            TestContext.Current.CancellationToken);

        await worker.Work(CancellationToken.None);

        platform.Received().UploadProgressNotification("vid.mp4", 50, 100);
    }
}