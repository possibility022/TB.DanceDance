using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TB.DanceDance.API.Contracts.Features.Videos;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models.Storage;
using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class UploadQueueServiceTests : IDisposable
{
    private readonly string stagingDirectory =
        Path.Combine(Path.GetTempPath(), $"upload-queue-{Guid.NewGuid():N}");

    [Fact]
    public async Task Enqueue_WaitsForStoreInitialization()
    {
        var factory = new TestVideosDbContextFactory();
        var scheduler = Substitute.For<IUploadScheduler>();
        var initializer = Substitute.For<IUploadStoreInitializer>();
        initializer
            .EnsureInitializedAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("not initialized")));
        var service = new UploadQueueService(
            factory,
            scheduler,
            new UploadQueueOptions { StagingDirectory = stagingDirectory },
            initializer,
            Substitute.For<IUploadQueueChangeNotifier>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EnqueueAsync(
                [],
                cancellationToken: TestContext.Current.CancellationToken));
        await scheduler.DidNotReceiveWithAnyArgs().ScheduleAsync();
    }

    [Fact]
    public async Task Enqueue_CopiesAllValidFiles_AndSchedulesOnce()
    {
        var factory = new TestVideosDbContextFactory();
        var scheduler = Substitute.For<IUploadScheduler>();
        var service = CreateService(factory, scheduler);
        var first = CreateFile([1, 2, 3]);
        var second = CreateFile([4, 5]);
        var stagingProgress = new List<UploadStagingProgress>();

        try
        {
            var results = await service.EnqueueAsync(
            [
                new UploadQueueRequest(first, SharingWithType.Private),
                new UploadQueueRequest(second, SharingWithType.Group, Guid.NewGuid())
            ], new SynchronousProgress<UploadStagingProgress>(stagingProgress.Add));

            Assert.All(results, result => Assert.True(result.Succeeded));
            await using var db = factory.CreateDbContext();
            var jobs = await db.VideosToUpload.OrderBy(x => x.CreatedAtUtc).ToListAsync();
            Assert.Equal(2, jobs.Count);
            Assert.All(jobs, job =>
            {
                Assert.Equal(UploadJobState.PendingRegistration, job.State);
                Assert.True(job.OwnsFile);
                Assert.True(File.Exists(job.FullFileName));
                Assert.Equal(Guid.Empty, job.RemoteVideoId);
            });
            await scheduler.Received(1).ScheduleAsync(Arg.Any<CancellationToken>());
            Assert.Contains(stagingProgress, value =>
                value.FileName == Path.GetFileName(first)
                && value.CopiedBytes == value.TotalBytes);
        }
        finally
        {
            File.Delete(first);
            File.Delete(second);
        }
    }

    [Fact]
    public async Task Enqueue_CancellationRemovesPreviouslyStagedFiles()
    {
        var factory = new TestVideosDbContextFactory();
        var scheduler = Substitute.For<IUploadScheduler>();
        var service = CreateService(factory, scheduler);
        var first = CreateFile([1, 2, 3]);
        var second = CreateFile([4, 5, 6]);
        using var cancellation = new CancellationTokenSource();
        var progress = new SynchronousProgress<UploadStagingProgress>(value =>
        {
            if (value.FileName == Path.GetFileName(second))
                cancellation.Cancel();
        });

        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.EnqueueAsync(
                    [
                        new UploadQueueRequest(first, SharingWithType.Private),
                        new UploadQueueRequest(second, SharingWithType.Private)
                    ],
                    progress,
                    cancellation.Token));

            Assert.False(Directory.Exists(stagingDirectory)
                         && Directory.EnumerateFiles(stagingDirectory).Any());
        }
        finally
        {
            File.Delete(first);
            File.Delete(second);
        }
    }

    [Fact]
    public async Task Enqueue_BadFile_DoesNotPreventLaterFile()
    {
        var factory = new TestVideosDbContextFactory();
        var scheduler = Substitute.For<IUploadScheduler>();
        var service = CreateService(factory, scheduler);
        var valid = CreateFile([7, 8, 9]);

        try
        {
            var results = await service.EnqueueAsync(
            [
                new UploadQueueRequest(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), SharingWithType.Private),
                new UploadQueueRequest(valid, SharingWithType.Event, Guid.NewGuid())
            ]);

            Assert.False(results[0].Succeeded);
            Assert.True(results[1].Succeeded);
            await using var db = factory.CreateDbContext();
            Assert.Single(await db.VideosToUpload.ToListAsync());
            await scheduler.Received(1).ScheduleAsync(Arg.Any<CancellationToken>());
        }
        finally
        {
            File.Delete(valid);
        }
    }

    [Fact]
    public async Task Retry_ReusesExistingRemoteVideo()
    {
        var factory = new TestVideosDbContextFactory();
        var scheduler = Substitute.For<IUploadScheduler>();
        var service = CreateService(factory, scheduler);
        var remoteId = Guid.NewGuid();

        await using (var db = factory.CreateDbContext())
        {
            db.VideosToUpload.Add(new VideosToUpload
            {
                Id = Guid.NewGuid(),
                FileName = "video.mp4",
                FullFileName = "video.mp4",
                RemoteVideoId = remoteId,
                State = UploadJobState.Failed
            });
            await db.SaveChangesAsync();
        }

        Guid id;
        await using (var db = factory.CreateDbContext())
            id = (await db.VideosToUpload.SingleAsync()).Id;

        await service.RetryAsync(id);

        await using (var db = factory.CreateDbContext())
        {
            var job = await db.VideosToUpload.SingleAsync();
            Assert.Equal(remoteId, job.RemoteVideoId);
            Assert.Equal(UploadJobState.PendingUpload, job.State);
        }
    }

    [Fact]
    public async Task Cancel_StopsCurrentWork_DeletesOwnedFile_AndReschedulesRemainingQueue()
    {
        var factory = new TestVideosDbContextFactory();
        var scheduler = Substitute.For<IUploadScheduler>();
        var service = CreateService(factory, scheduler);
        var source = CreateFile([1, 2, 3]);

        try
        {
            var result = Assert.Single(await service.EnqueueAsync(
            [
                new UploadQueueRequest(source, SharingWithType.Private)
            ]));
            string stagedPath;
            await using (var db = factory.CreateDbContext())
                stagedPath = (await db.VideosToUpload.SingleAsync()).FullFileName;

            await service.CancelAsync(result.JobId!.Value);

            await using (var db = factory.CreateDbContext())
                Assert.Equal(UploadJobState.Cancelled, (await db.VideosToUpload.SingleAsync()).State);
            Assert.False(File.Exists(stagedPath));
            await scheduler.Received(1).CancelAsync(Arg.Any<CancellationToken>());
            await scheduler.Received(2).ScheduleAsync(Arg.Any<CancellationToken>());
        }
        finally
        {
            File.Delete(source);
        }
    }

    private UploadQueueService CreateService(
        TestVideosDbContextFactory factory,
        IUploadScheduler scheduler) =>
        new(
            factory,
            scheduler,
            new UploadQueueOptions { StagingDirectory = stagingDirectory },
            Substitute.For<IUploadStoreInitializer>(),
            Substitute.For<IUploadQueueChangeNotifier>());

    private static string CreateFile(byte[] content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(path, content);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(stagingDirectory))
            Directory.Delete(stagingDirectory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private sealed class SynchronousProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
