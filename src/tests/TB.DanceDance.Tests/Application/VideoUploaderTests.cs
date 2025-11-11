using Application.Services;
using Domain;
using Infrastructure.Data;
using Infrastructure.Data.BlobStorage;
using Microsoft.EntityFrameworkCore;

namespace TB.DanceDance.Tests.Application;

public class VideoUploaderTests : BaseTestClass
{
    
    private static readonly SemaphoreSlim Locker = new(1);
    private bool lockedByThisClass = false;
    
    private readonly BlobStorageFixture blobStorageFixture;

    private BlobDataServiceFactory factory;
    private VideoUploaderService uploaderService = null!;

    public VideoUploaderTests(DanceDbFixture dbContextFixture, BlobStorageFixture blobStorageFixture) : base(dbContextFixture)
    {
        this.blobStorageFixture = blobStorageFixture;
        this.factory = new BlobDataServiceFactory(blobStorageFixture.GetConnectionString());
    }

    protected override ValueTask BeforeDispose(DanceDbContext runtimeDbContext)
    {
        if (lockedByThisClass)
            Locker.Release(1);
        
        return base.BeforeDispose(runtimeDbContext);
    }

    protected override async ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        var isIn = await Locker.WaitAsync(TimeSpan.FromSeconds(10));
        lockedByThisClass = isIn;
        
        if (!isIn)
        {
            throw new("Could not acquire lock");
        }
        
        factory = new BlobDataServiceFactory(blobStorageFixture.GetConnectionString());
        this.uploaderService = new VideoUploaderService(factory, runtimeDbContext);
    }

    private async Task MakeAllExistingVideosIneligible()
    {
        var existing = SeedDbContext.Videos.ToList();
        if (existing.Count > 0)
        {
            foreach (var vid in existing)
            {
                vid.Converted = true;
                vid.LockedTill = DateTime.UtcNow.AddDays(365 * 10);
            }
            await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task GetNextVideoToTransformAsync_DoesNotReturnLockedOrConverted()
    {
        await MakeAllExistingVideosIneligible();
        var user = new UserDataBuilder().Build();
        var locked = new VideoDataBuilder().UploadedBy(user).SharedAt(DateTime.UtcNow.AddMinutes(-1)).Build();
        locked.LockedTill = DateTime.UtcNow.AddHours(1);
        var converted = new VideoDataBuilder().UploadedBy(user).SharedAt(DateTime.UtcNow.AddMinutes(-2)).Converted(true).Build();
        SeedDbContext.AddRange(user, locked, converted);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var next = await uploaderService.GetNextVideoToTransformAsync(TestContext.Current.CancellationToken);
        if (next is not null)
        {
            Assert.NotEqual(locked.Id, next.Id);
            Assert.NotEqual(converted.Id, next.Id);
        }
    }

    [Fact]
    public async Task GetNextVideoToTransformAsync_SkipsLockedAndConverted_AndLocksNewest()
    {
        await MakeAllExistingVideosIneligible();
        var user = new UserDataBuilder().Build();
        var vLocked = new VideoDataBuilder()
            .UploadedBy(user)
            .SharedAt(DateTime.UtcNow.AddMinutes(-30))
            .Build();
        vLocked.LockedTill = DateTime.UtcNow.AddHours(1);

        var vConverted = new VideoDataBuilder()
            .UploadedBy(user)
            .SharedAt(DateTime.UtcNow.AddMinutes(-10))
            .Converted(true)
            .Build();

        var vEligible = new VideoDataBuilder()
            .UploadedBy(user)
            .SharedAt(DateTime.UtcNow.AddMinutes(-5))
            .Build();

        SeedDbContext.AddRange(user, vLocked, vConverted, vEligible);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var next = await uploaderService.GetNextVideoToTransformAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(next);
        Assert.Equal(vEligible.Id, next!.Id);
        Assert.NotNull(next.LockedTill);
        Assert.True(next.LockedTill!.Value > DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, next.LockedTill!.Value.Kind);
    }

    [Fact]
    public async Task UpdateVideoInformation_UpdatesFields_AndSavesMetadata()
    {
        var user = new UserDataBuilder().Build();
        var v = new VideoDataBuilder().UploadedBy(user).Build();
        SeedDbContext.AddRange(user, v);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var duration = TimeSpan.FromMinutes(2);
        var recorded = DateTime.UtcNow.AddDays(-2);
        var metadata = new byte[] { 1, 2, 3 };

        var ok = await uploaderService.UpdateVideoInformation(v.Id, duration, recorded, metadata, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        Assert.True(ok);
        var updated = await SeedDbContext.Videos.AsNoTracking().FirstAsync(x => x.Id == v.Id, TestContext.Current.CancellationToken);
        Assert.Equal(duration, updated.Duration);
        // Use tolerance because some providers truncate sub-millisecond precision
        Assert.True((updated.RecordedDateTime - recorded).Duration() < TimeSpan.FromMilliseconds(5));
        Assert.Equal(DateTimeKind.Utc, updated.RecordedDateTime.Kind);
        Assert.True(await SeedDbContext.VideoMetadata.AnyAsync(m => m.VideoId == v.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateVideoInformation_DoesNotAddMetadata_WhenNull()
    {
        var user = new UserDataBuilder().Build();
        var v = new VideoDataBuilder().UploadedBy(user).Build();
        SeedDbContext.AddRange(user, v);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ok = await uploaderService.UpdateVideoInformation(v.Id, TimeSpan.FromSeconds(10), DateTime.UtcNow, null, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
        Assert.True(ok);
        Assert.False(await SeedDbContext.VideoMetadata.AnyAsync(m => m.VideoId == v.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UploadConvertedVideoAsync_ReturnsNull_WhenVideoMissing()
    {
        var result = await uploaderService.UploadConvertedVideoAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task UploadConvertedVideoAsync_ReturnsNull_WhenBlobIdNull()
    {
        var user = new UserDataBuilder().Build();
        var v = new VideoDataBuilder().UploadedBy(user).WithBlobId(null).Build();
        SeedDbContext.AddRange(user, v);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await uploaderService.UploadConvertedVideoAsync(v.Id, TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task UploadConvertedVideoAsync_ReturnsNull_WhenBlobNotInPublishedContainer()
    {
        var user = new UserDataBuilder().Build();
        var v = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString()).Build();
        SeedDbContext.AddRange(user, v);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await uploaderService.UploadConvertedVideoAsync(v.Id, TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task UploadConvertedVideoAsync_SetsConvertedTrue_WhenBlobExists()
    {
        var user = new UserDataBuilder().Build();
        var blobId = Guid.NewGuid().ToString();
        var v = new VideoDataBuilder().UploadedBy(user).WithBlobId(blobId).Build();
        SeedDbContext.AddRange(user, v);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Upload a blob to the published videos container so service sees it
        var published = factory.GetBlobDataService(BlobContainer.Videos);
        await published.Upload(blobId, new MemoryStream(new byte[] { 9, 9, 9 }));

        var result = await uploaderService.UploadConvertedVideoAsync(v.Id, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
        Assert.Equal(v.Id, result);
        var updated = await SeedDbContext.Videos.AsNoTracking().FirstAsync(x => x.Id == v.Id, TestContext.Current.CancellationToken);
        Assert.True(updated.Converted);
    }

    [Fact]
    public void GetUploadSasUri_ReturnsSharedBlob()
    {
        var shared = uploaderService.GetUploadSasUri();
        Assert.NotNull(shared);
        Assert.False(string.IsNullOrWhiteSpace(shared.BlobId));
        Assert.True(shared.ExpiresAt > DateTimeOffset.Now);
        Assert.True(shared.Sas.IsAbsoluteUri);
    }

    [Fact]
    public void GetUploadSasUri_Throws_OnEmptyBlobId()
    {
        Assert.Throws<ArgumentNullException>(() => uploaderService.GetUploadSasUri(" "));
    }

    [Fact]
    public async Task GetSasForConvertedVideoAsync_ReturnsNull_WhenVideoMissing()
    {
        var sas = await uploaderService.GetSasForConvertedVideoAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        Assert.Null(sas);
    }

    [Fact]
    public async Task GetSasForConvertedVideoAsync_AssignsBlobId_AndPersists()
    {
        var user = new UserDataBuilder().Build();
        var v = new VideoDataBuilder().UploadedBy(user).Build();
        SeedDbContext.AddRange(user, v);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sas = await uploaderService.GetSasForConvertedVideoAsync(v.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(sas);
        SeedDbContext.ChangeTracker.Clear();
        var updated = await SeedDbContext.Videos.AsNoTracking().FirstAsync(x => x.Id == v.Id, TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrWhiteSpace(updated.BlobId));
        Assert.Equal(updated.BlobId, sas!.BlobId);
        Assert.True(sas.Sas.IsAbsoluteUri);
    }

    [Fact]
    public void GetVideoSas_ReturnsUri()
    {
        var uri = uploaderService.GetVideoSas(Guid.NewGuid().ToString());
        Assert.True(uri.IsAbsoluteUri);
    }
}