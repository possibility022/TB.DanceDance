using Azure;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Net;
using TB.DanceDance.API.Contracts.Features.Videos;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models.Storage;
using TB.DanceDance.Mobile.Library.Services.Auth;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.Library.Services.Network;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class UploadWorkerTests
{
    private static (
        UploadWorker Worker,
        TestVideosDbContextFactory Factory,
        IVideoUploader Uploader,
        IDanceHttpApiClient Api,
        ITokenProviderService TokenProvider) CreateSut(bool authenticated = true)
    {
        var factory = new TestVideosDbContextFactory();
        var uploader = Substitute.For<IVideoUploader>();
        var api = Substitute.For<IDanceHttpApiClient>();
        var tokenProvider = Substitute.For<ITokenProviderService>();
        var storeInitializer = Substitute.For<IUploadStoreInitializer>();
        tokenProvider.GetAccessTokenSilently().Returns(authenticated ? "token" : null);
        return (
            new UploadWorker(
                factory,
                uploader,
                api,
                tokenProvider,
                new UploadExecutionGate(),
                storeInitializer,
                Substitute.For<IUploadQueueChangeNotifier>()),
            factory,
            uploader,
            api,
            tokenProvider);
    }

    [Fact]
    public async Task Work_NoJobs_Completes()
    {
        var sut = CreateSut();

        var result = await sut.Worker.Work(null, TestContext.Current.CancellationToken);

        Assert.Equal(UploadRunResult.Complete, result);
        await sut.Uploader.DidNotReceiveWithAnyArgs()
            .Upload(null!, null, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Work_RegistersAndUploadsEveryPendingJob()
    {
        var sut = CreateSut();
        var first = CreateJob();
        var second = CreateJob();
        await AddJobs(sut.Factory, first, second);
        sut.Api.GetUploadInformation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<SharingWithType>(),
                Arg.Any<Guid?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(
                UploadInformation(),
                UploadInformation());

        var result = await sut.Worker.Work(null, TestContext.Current.CancellationToken);

        Assert.Equal(UploadRunResult.Complete, result);
        await sut.Uploader.Received(2)
            .Upload(Arg.Any<VideosToUpload>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>());
        await using var db = sut.Factory.CreateDbContext();
        Assert.All(await db.VideosToUpload.ToListAsync(), job =>
        {
            Assert.Equal(UploadJobState.Completed, job.State);
            Assert.True(job.Uploaded);
        });
    }

    [Fact]
    public async Task Work_RetryDoesNotRegisterASecondServerVideo()
    {
        var sut = CreateSut();
        var job = CreateJob();
        await AddJobs(sut.Factory, job);
        var uploadInformation = UploadInformation();
        sut.Api.GetUploadInformation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<SharingWithType>(),
                Arg.Any<Guid?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(uploadInformation);
        sut.Uploader.Upload(
                Arg.Any<VideosToUpload>(),
                Arg.Any<IProgress<long>>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException(new RequestFailedException(500, "temporary")),
                Task.CompletedTask);

        Assert.Equal(
            UploadRunResult.Retry,
            await sut.Worker.Work(null, TestContext.Current.CancellationToken));

        await using (var db = sut.Factory.CreateDbContext())
        {
            var persisted = await db.VideosToUpload.SingleAsync();
            Assert.Equal(uploadInformation.VideoId, persisted.RemoteVideoId);
            persisted.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }

        Assert.Equal(
            UploadRunResult.Complete,
            await sut.Worker.Work(null, TestContext.Current.CancellationToken));
        await sut.Api.Received(1).GetUploadInformation(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<SharingWithType>(),
            Arg.Any<Guid?>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Work_ExpiredSas_PersistsRefreshBeforeUpload()
    {
        var sut = CreateSut();
        var job = CreateJob(registered: true);
        job.SasExpireAt = DateTime.UtcNow.AddMinutes(-1);
        await AddJobs(sut.Factory, job);
        var refreshed = new RefreshUploadUrlResponse
        {
            VideoId = job.RemoteVideoId,
            Sas = "https://example/new-sas",
            ExpireAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        sut.Api.RefreshUploadUrl(job.RemoteVideoId, Arg.Any<CancellationToken>()).Returns(refreshed);

        await sut.Worker.Work(null, TestContext.Current.CancellationToken);

        await using var db = sut.Factory.CreateDbContext();
        var persisted = await db.VideosToUpload.SingleAsync();
        Assert.Equal(refreshed.Sas, persisted.Sas);
        Assert.Equal(UploadJobState.Completed, persisted.State);
    }

    [Fact]
    public async Task Work_ProcessRestart_ResumesJobLeftUploading()
    {
        var sut = CreateSut();
        var job = CreateJob(registered: true);
        job.State = UploadJobState.Uploading;
        await AddJobs(sut.Factory, job);

        var result = await sut.Worker.Work(null, TestContext.Current.CancellationToken);

        Assert.Equal(UploadRunResult.Complete, result);
        await sut.Api.DidNotReceiveWithAnyArgs().GetUploadInformation(
            null!, null!, default, null, default);
        await using var db = sut.Factory.CreateDbContext();
        Assert.Equal(UploadJobState.Completed, (await db.VideosToUpload.SingleAsync()).State);
    }

    [Fact]
    public async Task Work_MissingFileFailsPermanently_AndContinues()
    {
        var sut = CreateSut();
        var missing = CreateJob(registered: true);
        missing.FullFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var valid = CreateJob(registered: true);
        await AddJobs(sut.Factory, missing, valid);
        sut.Uploader.Upload(
                Arg.Is<VideosToUpload>(job => job.Id == missing.Id),
                Arg.Any<IProgress<long>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new FileNotFoundException()));

        var result = await sut.Worker.Work(null, TestContext.Current.CancellationToken);

        Assert.Equal(UploadRunResult.Complete, result);
        await using var db = sut.Factory.CreateDbContext();
        Assert.Equal(UploadJobState.Failed, (await db.VideosToUpload.FindAsync(missing.Id))!.State);
        Assert.Equal(UploadJobState.Completed, (await db.VideosToUpload.FindAsync(valid.Id))!.State);
    }

    [Fact]
    public async Task Work_NoSilentToken_WaitsForAuthenticationWithoutCallingApi()
    {
        var sut = CreateSut(authenticated: false);
        var job = CreateJob();
        await AddJobs(sut.Factory, job);

        var result = await sut.Worker.Work(null, TestContext.Current.CancellationToken);

        Assert.Equal(UploadRunResult.Retry, result);
        await sut.Api.DidNotReceiveWithAnyArgs().GetUploadInformation(
            null!, null!, default, null, default);
        await using var db = sut.Factory.CreateDbContext();
        Assert.Equal(
            UploadJobState.WaitingForAuthentication,
            (await db.VideosToUpload.SingleAsync()).State);
    }

    [Fact]
    public async Task Work_ApiUnauthorized_ReturnsRetryWithoutSpinning()
    {
        var sut = CreateSut();
        await AddJobs(sut.Factory, CreateJob());
        sut.Api.GetUploadInformation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<SharingWithType>(),
                Arg.Any<Guid?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ProduceUploadUrlResponse?>(
                new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized)));

        var result = await sut.Worker.Work(null, TestContext.Current.CancellationToken);

        Assert.Equal(UploadRunResult.Retry, result);
        await sut.Api.Received(1).GetUploadInformation(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<SharingWithType>(),
            Arg.Any<Guid?>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await using var db = sut.Factory.CreateDbContext();
        Assert.Equal(
            UploadJobState.WaitingForAuthentication,
            (await db.VideosToUpload.SingleAsync()).State);
    }

    [Fact]
    public async Task Work_ForwardsExactProgress()
    {
        var sut = CreateSut();
        var job = CreateJob(registered: true);
        job.FileSize = 100;
        await AddJobs(sut.Factory, job);
        sut.Uploader.Upload(
                Arg.Any<VideosToUpload>(),
                Arg.Any<IProgress<long>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                call.Arg<IProgress<long>>().Report(40);
                return Task.CompletedTask;
            });
        var messages = new List<UploadProgressEvent>();

        await sut.Worker.Work(
            new InlineProgress<UploadProgressEvent>(messages.Add),
            TestContext.Current.CancellationToken);

        Assert.Contains(messages, message => message.SendBytes == 40 && message.FileSize == 100);
    }

    private static VideosToUpload CreateJob(bool registered = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            FileName = $"{Guid.NewGuid():N}.mp4",
            VideoName = "Dance video",
            FullFileName = Path.GetTempFileName(),
            FileSize = 10,
            State = registered ? UploadJobState.PendingUpload : UploadJobState.PendingRegistration,
            RemoteVideoId = registered ? Guid.NewGuid() : Guid.Empty,
            Sas = registered ? "https://example/upload?sas=1" : string.Empty,
            SasExpireAt = registered ? DateTime.UtcNow.AddHours(1) : default,
            SharingWithType = SharingWithType.Private,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

    private static ProduceUploadUrlResponse UploadInformation() =>
        new()
        {
            Sas = "https://example/upload?sas=1",
            VideoId = Guid.NewGuid(),
            ExpireAt = DateTimeOffset.UtcNow.AddHours(1)
        };

    private static async Task AddJobs(
        TestVideosDbContextFactory factory,
        params VideosToUpload[] jobs)
    {
        await using var db = factory.CreateDbContext();
        db.VideosToUpload.AddRange(jobs);
        await db.SaveChangesAsync();
    }
}
