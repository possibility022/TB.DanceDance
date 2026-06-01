using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;
using TB.DanceDance.Utilities.Infrastructure;
using TB.DanceDance.Videos.Contracts;

namespace TB.DanceDance.Tests.Features.Videos;

/// <summary>
/// Converter-flow handlers (Videos module): pick-next-to-convert, update-info, upload-converted, and
/// re-issue publish SAS. Note the old <c>VideoUploaderService.GetUploadSasUri</c>/<c>GetVideoSas</c>
/// helpers are not exposed as handlers (they are internal to <see cref="CreateVideoUploadCommand"/> /
/// streaming), so those two micro-tests have no module-surface equivalent and are not ported.
/// </summary>
public class VideoUploaderTests : BaseTestClass
{
    private static readonly SemaphoreSlim Locker = new(1);
    private bool lockedByThisClass;

    private readonly BlobStorageFixture blobStorageFixture;
    private IBlobDataServiceFactory factory = null!;

    public VideoUploaderTests(DanceDbFixture dbContextFixture, BlobStorageFixture blobStorageFixture) : base(dbContextFixture)
    {
        this.blobStorageFixture = blobStorageFixture;
    }

    protected override string BlobConnectionString => blobStorageFixture.GetConnectionString();

    protected override async ValueTask Initialize()
    {
        var isIn = await Locker.WaitAsync(TimeSpan.FromSeconds(10));
        lockedByThisClass = isIn;
        if (!isIn)
            throw new("Could not acquire lock");

        factory = new BlobDataServiceFactory(blobStorageFixture.GetConnectionString());
    }

    protected override ValueTask BeforeDispose()
    {
        if (lockedByThisClass)
            Locker.Release(1);
        return ValueTask.CompletedTask;
    }

    private async Task MakeAllExistingVideosIneligible()
    {
        var existing = await SeedVideosContext.Videos.ToListAsync(TestContext.Current.CancellationToken);
        if (existing.Count > 0)
        {
            foreach (var vid in existing)
            {
                vid.Converted = true;
                vid.LockedTill = DateTime.UtcNow.AddDays(365 * 10);
            }
            await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task GetNextVideoToTransformAsync_DoesNotReturnLockedOrConverted()
    {
        await MakeAllExistingVideosIneligible();
        var user = new UserDataBuilder().Build();
        var locked = new VideoDataBuilder().UploadedBy(user).Build();
        locked.LockedTill = DateTime.UtcNow.AddHours(1);
        var converted = new VideoDataBuilder().UploadedBy(user).Converted(true).Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(locked, converted);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var next = await Send(new GetNextVideoToConvertQuery(), TestContext.Current.CancellationToken);
        if (next is not null)
        {
            Assert.NotEqual(locked.Id, next.Id);
            Assert.NotEqual(converted.Id, next.Id);
        }
    }

    [Fact]
    public async Task GetNextVideoToTransformAsync_SkipsLockedAndConverted_AndLocksEligible()
    {
        await MakeAllExistingVideosIneligible();
        var user = new UserDataBuilder().Build();
        var vLocked = new VideoDataBuilder().UploadedBy(user).Build();
        vLocked.LockedTill = DateTime.UtcNow.AddHours(1);
        var vConverted = new VideoDataBuilder().UploadedBy(user).Converted(true).Build();
        var vEligible = new VideoDataBuilder().UploadedBy(user).Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(vLocked, vConverted, vEligible);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var next = await Send(new GetNextVideoToConvertQuery(), TestContext.Current.CancellationToken);

        Assert.NotNull(next);
        Assert.Equal(vEligible.Id, next!.Id);

        // VideoToConvertDto does not expose LockedTill; verify the row was locked on the entity.
        var locked = await SeedVideosContext.Videos.AsNoTracking().FirstAsync(v => v.Id == next.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(locked.LockedTill);
        Assert.True(locked.LockedTill!.Value > DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, locked.LockedTill!.Value.Kind);
    }

    [Fact]
    public async Task UpdateVideoInformation_UpdatesFields_AndSavesMetadata()
    {
        var user = new UserDataBuilder().Build();
        var v = new VideoDataBuilder().UploadedBy(user).Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(v);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var duration = TimeSpan.FromMinutes(2);
        var recorded = DateTime.UtcNow.AddDays(-2);
        var metadata = new byte[] { 1, 2, 3 };

        var ok = await Send(new UpdateVideoInformationCommand { VideoId = v.Id, Duration = duration, Recorded = recorded, Metadata = metadata },
            TestContext.Current.CancellationToken);

        Assert.True(ok);
        var updated = await SeedVideosContext.Videos.AsNoTracking().FirstAsync(x => x.Id == v.Id, TestContext.Current.CancellationToken);
        Assert.Equal(duration, updated.Duration);
        Assert.True((updated.RecordedDateTime - recorded).Duration() < TimeSpan.FromMilliseconds(5));
        Assert.Equal(DateTimeKind.Utc, updated.RecordedDateTime.Kind);
        Assert.True(await SeedVideosContext.VideoMetadata.AnyAsync(m => m.VideoId == v.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateVideoInformation_DoesNotAddMetadata_WhenNull()
    {
        var user = new UserDataBuilder().Build();
        var v = new VideoDataBuilder().UploadedBy(user).Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(v);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ok = await Send(new UpdateVideoInformationCommand { VideoId = v.Id, Duration = TimeSpan.FromSeconds(10), Recorded = DateTime.UtcNow, Metadata = null },
            TestContext.Current.CancellationToken);
        Assert.True(ok);
        Assert.False(await SeedVideosContext.VideoMetadata.AnyAsync(m => m.VideoId == v.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UploadConvertedVideoAsync_ReturnsNull_WhenVideoMissing()
    {
        var result = await Send(new UploadConvertedVideoCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task UploadConvertedVideoAsync_ReturnsNull_WhenBlobIdNull()
    {
        var user = new UserDataBuilder().Build();
        var v = new VideoDataBuilder().UploadedBy(user).WithBlobId(null).Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(v);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new UploadConvertedVideoCommand(v.Id), TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task UploadConvertedVideoAsync_ReturnsNull_WhenBlobNotInPublishedContainer()
    {
        var user = new UserDataBuilder().Build();
        var v = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString()).Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(v);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new UploadConvertedVideoCommand(v.Id), TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task UploadConvertedVideoAsync_SetsConvertedTrue_WhenBlobExists()
    {
        var user = new UserDataBuilder().Build();
        var blobId = Guid.NewGuid().ToString();
        var v = new VideoDataBuilder().UploadedBy(user).WithBlobId(blobId).Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(v);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var published = factory.GetBlobDataService(BlobContainer.Videos);
        await published.Upload(blobId, new MemoryStream(new byte[] { 9, 9, 9 }));

        var result = await Send(new UploadConvertedVideoCommand(v.Id), TestContext.Current.CancellationToken);
        Assert.Equal(v.Id, result);
        var updated = await SeedVideosContext.Videos.AsNoTracking().FirstAsync(x => x.Id == v.Id, TestContext.Current.CancellationToken);
        Assert.True(updated.Converted);
    }

    [Fact]
    public async Task GetSasForConvertedVideoAsync_ReturnsNull_WhenVideoMissing()
    {
        var sas = await Send(new GetPublishSasQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);
        Assert.Null(sas);
    }

    [Fact]
    public async Task GetSasForConvertedVideoAsync_AssignsBlobId_AndPersists()
    {
        var user = new UserDataBuilder().Build();
        var v = new VideoDataBuilder().UploadedBy(user).WithBlobId(null).Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(v);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sas = await Send(new GetPublishSasQuery(v.Id), TestContext.Current.CancellationToken);
        Assert.NotNull(sas);
        var updated = await SeedVideosContext.Videos.AsNoTracking().FirstAsync(x => x.Id == v.Id, TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrWhiteSpace(updated.BlobId));
        Assert.Equal(updated.BlobId, sas!.BlobId);
        Assert.True(sas.Sas.IsAbsoluteUri);
    }

    [Fact]
    public async Task UploadConvertedVideoAsync_CalculatesAndStoresBlobSizes()
    {
        var user = new UserDataBuilder().Build();
        var sourceBlobId = Guid.NewGuid().ToString();
        var convertedBlobId = Guid.NewGuid().ToString();
        var v = new VideoDataBuilder().UploadedBy(user).WithSourceBlobId(sourceBlobId).WithBlobId(convertedBlobId).Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(v);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sourceData = new byte[1024];
        Array.Fill(sourceData, (byte)1);
        var toConvert = factory.GetBlobDataService(BlobContainer.VideosToConvert);
        await toConvert.Upload(sourceBlobId, new MemoryStream(sourceData));

        var convertedData = new byte[2048];
        Array.Fill(convertedData, (byte)2);
        var published = factory.GetBlobDataService(BlobContainer.Videos);
        await published.Upload(convertedBlobId, new MemoryStream(convertedData));

        var result = await Send(new UploadConvertedVideoCommand(v.Id), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var updated = await SeedVideosContext.Videos.AsNoTracking().FirstAsync(x => x.Id == v.Id, TestContext.Current.CancellationToken);
        Assert.True(updated.Converted);
        Assert.Equal(1024, updated.SourceBlobSize);
        Assert.Equal(2048, updated.ConvertedBlobSize);
    }

    [Fact]
    public async Task UploadConvertedVideoAsync_ContinuesWithZeroSizes_WhenSizeCalculationFails()
    {
        var user = new UserDataBuilder().Build();
        var sourceBlobId = Guid.NewGuid().ToString();
        var convertedBlobId = Guid.NewGuid().ToString();
        var v = new VideoDataBuilder().UploadedBy(user).WithSourceBlobId(sourceBlobId).WithBlobId(convertedBlobId).Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(v);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Only the converted blob exists; source size calculation throws and is swallowed.
        var convertedData = new byte[512];
        var published = factory.GetBlobDataService(BlobContainer.Videos);
        await published.Upload(convertedBlobId, new MemoryStream(convertedData));

        var result = await Send(new UploadConvertedVideoCommand(v.Id), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var updated = await SeedVideosContext.Videos.AsNoTracking().FirstAsync(x => x.Id == v.Id, TestContext.Current.CancellationToken);
        Assert.True(updated.Converted);
        Assert.Equal(0, updated.SourceBlobSize);
        Assert.Equal(0, updated.ConvertedBlobSize);
    }
}
