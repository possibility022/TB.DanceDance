using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models.Storage;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class UploadQueueSchemaUpgraderTests
{
    [Fact]
    public async Task Upgrade_PreservesLegacyPendingRows()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db3");
        var options = new DbContextOptionsBuilder<VideosDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        try
        {
            await using (var db = new VideosDbContext(options))
            {
                await db.Database.ExecuteSqlRawAsync("""
                    CREATE TABLE "VideosToUpload" (
                        "Id" TEXT NOT NULL CONSTRAINT "PK_VideosToUpload" PRIMARY KEY,
                        "FullFileName" TEXT NOT NULL,
                        "FileName" TEXT NOT NULL,
                        "Uploaded" INTEGER NOT NULL,
                        "RemoteVideoId" TEXT NOT NULL,
                        "Sas" TEXT NOT NULL,
                        "SasExpireAt" TEXT NOT NULL
                    );
                    """, TestContext.Current.CancellationToken);

                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO "VideosToUpload"
                        ("Id", "FullFileName", "FileName", "Uploaded", "RemoteVideoId", "Sas", "SasExpireAt")
                    VALUES
                        ({Guid.NewGuid()}, 'legacy.mp4', 'legacy.mp4', 0, {Guid.NewGuid()},
                         'https://example/sas', {DateTime.UtcNow.AddHours(1)});
                    """, TestContext.Current.CancellationToken);

                await UploadQueueSchemaUpgrader.UpgradeAsync(db, TestContext.Current.CancellationToken);
            }

            await using (var verification = new VideosDbContext(options))
            {
                var job = await verification.VideosToUpload.SingleAsync(TestContext.Current.CancellationToken);
                Assert.Equal(UploadJobState.PendingUpload, job.State);
                Assert.Equal("legacy.mp4", job.VideoName);
                Assert.False(job.OwnsFile);
            }
        }
        finally
        {
            File.Delete(databasePath);
        }
    }
}
