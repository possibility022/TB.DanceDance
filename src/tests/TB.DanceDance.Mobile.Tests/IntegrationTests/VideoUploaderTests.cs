using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Devices;
using NSubstitute;
using System.Threading.Channels;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models.Storage;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.Library.Services.Network;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class VideoUploaderTests
{
    private static VideosDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<VideosDbContext>()
            .UseInMemoryDatabase(databaseName: $"vids-{Guid.NewGuid()}")
            .Options;
        return new VideosDbContext(options);
    }

    private static (VideoUploader uploader, VideosDbContext db, IDanceHttpApiClient api) CreateSut()
    {
        var db = CreateDb();
        var api = Substitute.For<IDanceHttpApiClient>();
        var channel = Channel.CreateUnbounded<UploadProgressEvent>();
        var resolver = new NetworkAddressResolver(DevicePlatform.WinUI);
        var uploader = new VideoUploader(api, db, channel, resolver);
        return (uploader, db, api);
    }

    [Fact]
    public async Task AddToUploadList_WithNullName_UsesFileName_PersistsAndCallsApi()
    {
        var (uploader, db, api) = CreateSut();

        // Arrange temp file
        var temp = Path.GetTempFileName();
        try
        {
            var groupId = Guid.NewGuid();
            var uploadInfo = new UploadVideoInformationResponse
            {
                Sas = "https://example/sas", VideoId = Guid.NewGuid(), ExpireAt = DateTimeOffset.UtcNow.AddHours(2)
            };
            api.GetUploadInformation(Arg.Any<string>(), Arg.Any<string>(), SharingWithType.Group, groupId,
                    Arg.Any<DateTime>())
                .Returns(Task.FromResult<UploadVideoInformationResponse?>(uploadInfo));

            // Act
            await uploader.AddToUploadList(null, temp, groupId, CancellationToken.None);

            // Assert persisted
            var row = await db.VideosToUpload.FirstOrDefaultAsync(r => r.FullFileName == temp,
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(row);
            Assert.Equal(Path.GetFileName(temp), row!.FileName);
            Assert.Equal(uploadInfo.Sas, row.Sas);
            Assert.Equal(uploadInfo.VideoId, row.RemoteVideoId);
            Assert.True(row.SasExpireAt > DateTime.UtcNow);

            await api.Received(1).GetUploadInformation(Path.GetFileName(temp), Path.GetFileName(temp),
                SharingWithType.Group, groupId, Arg.Any<DateTime>());
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public async Task AddToUploadList_Skips_WhenExistingUploadedTrue()
    {
        var (uploader, db, api) = CreateSut();
        var temp = Path.GetTempFileName();
        try
        {
            var existing = new VideosToUpload
            {
                Id = Guid.NewGuid(),
                FullFileName = temp,
                FileName = Path.GetFileName(temp),
                Uploaded = true,
                RemoteVideoId = Guid.NewGuid(),
                Sas = "s",
                SasExpireAt = DateTime.UtcNow.AddHours(1)
            };
            db.VideosToUpload.Add(existing);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            await uploader.AddToUploadList("ignored", temp, Guid.NewGuid(), CancellationToken.None);

            await api.DidNotReceiveWithAnyArgs()
                .GetUploadInformation(null!, null!, default!, Guid.Empty, default);
            Assert.Equal(1, db.VideosToUpload.Count());
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public async Task UploadVideoToGroup_PassesCorrectSharingType()
    {
        var (uploader, db, api) = CreateSut();
        var temp = Path.GetTempFileName();
        try
        {
            var groupId = Guid.NewGuid();
            var uploadInfo = new UploadVideoInformationResponse
            {
                Sas = "https://example/sas", VideoId = Guid.NewGuid(), ExpireAt = DateTimeOffset.UtcNow.AddHours(1)
            };

            api.GetUploadInformation(Arg.Any<string>(), Arg.Any<string>(), SharingWithType.Group, groupId,
                    Arg.Any<DateTime>())
                .Returns(Task.FromResult<UploadVideoInformationResponse?>(uploadInfo));

            await uploader.UploadVideoToGroup(temp, groupId, CancellationToken.None);

            await api.Received(1).GetUploadInformation(Path.GetFileName(temp), Path.GetFileName(temp),
                SharingWithType.Group, groupId, Arg.Any<DateTime>());
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public async Task UploadVideoToEvent_PassesCorrectSharingType()
    {
        var (uploader, db, api) = CreateSut();
        var temp = Path.GetTempFileName();
        try
        {
            var eventId = Guid.NewGuid();
            var uploadInfo = new UploadVideoInformationResponse
            {
                Sas = "https://example/sas", VideoId = Guid.NewGuid(), ExpireAt = DateTimeOffset.UtcNow.AddHours(1)
            };

            api.GetUploadInformation(Arg.Any<string>(), Arg.Any<string>(), SharingWithType.Event, eventId,
                    Arg.Any<DateTime>())
                .Returns(Task.FromResult<UploadVideoInformationResponse?>(uploadInfo));

            await uploader.UploadVideoToEvent(temp, eventId, CancellationToken.None);

            await api.Received(1).GetUploadInformation(Path.GetFileName(temp), Path.GetFileName(temp),
                SharingWithType.Event, eventId, Arg.Any<DateTime>());
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public async Task AddToUploadList_Throws_WhenUploadInformationNull()
    {
        var (uploader, db, api) = CreateSut();
        var temp = Path.GetTempFileName();
        try
        {
            var groupId = Guid.NewGuid();
            api.GetUploadInformation(Arg.Any<string>(), Arg.Any<string>(), SharingWithType.Group, groupId,
                    Arg.Any<DateTime>())
                .Returns(Task.FromResult<UploadVideoInformationResponse?>(null));

            await Assert.ThrowsAsync<Exception>(() =>
                uploader.AddToUploadList("n", temp, groupId, CancellationToken.None));
        }
        finally
        {
            File.Delete(temp);
        }
    }
}