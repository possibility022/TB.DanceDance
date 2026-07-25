using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace TB.DanceDance.Mobile.Library.Data;

public static class UploadQueueSchemaUpgrader
{
    private const string TableName = "VideosToUpload";

    public static async Task UpgradeAsync(VideosDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        try
        {
            var columns = await ReadColumnsAsync(connection, cancellationToken);
            await AddColumnAsync(connection, columns, "State", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
            await AddColumnAsync(connection, columns, "SharingWithType", "INTEGER NOT NULL DEFAULT 3", cancellationToken);
            await AddColumnAsync(connection, columns, "SharedWithId", "TEXT NULL", cancellationToken);
            await AddColumnAsync(connection, columns, "VideoName", "TEXT NOT NULL DEFAULT ''", cancellationToken);
            await AddColumnAsync(connection, columns, "RecordedTimeUtc", "TEXT NOT NULL DEFAULT '0001-01-01 00:00:00'", cancellationToken);
            await AddColumnAsync(connection, columns, "FileSize", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
            await AddColumnAsync(connection, columns, "UploadedBytes", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
            await AddColumnAsync(connection, columns, "AttemptCount", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
            await AddColumnAsync(connection, columns, "NextAttemptAtUtc", "TEXT NULL", cancellationToken);
            await AddColumnAsync(connection, columns, "LastError", "TEXT NULL", cancellationToken);
            await AddColumnAsync(connection, columns, "CancellationRequested", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
            await AddColumnAsync(connection, columns, "OwnsFile", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
            await AddColumnAsync(connection, columns, "CreatedAtUtc", "TEXT NOT NULL DEFAULT '0001-01-01 00:00:00'", cancellationToken);
            await AddColumnAsync(connection, columns, "UpdatedAtUtc", "TEXT NOT NULL DEFAULT '0001-01-01 00:00:00'", cancellationToken);

            await ExecuteAsync(connection, """
                UPDATE "VideosToUpload"
                SET "State" = CASE WHEN "Uploaded" = 1 THEN 6 ELSE 1 END,
                    "VideoName" = CASE WHEN "VideoName" = '' THEN "FileName" ELSE "VideoName" END,
                    "CreatedAtUtc" = CASE WHEN "CreatedAtUtc" = '0001-01-01 00:00:00' THEN CURRENT_TIMESTAMP ELSE "CreatedAtUtc" END,
                    "UpdatedAtUtc" = CASE WHEN "UpdatedAtUtc" = '0001-01-01 00:00:00' THEN CURRENT_TIMESTAMP ELSE "UpdatedAtUtc" END
                WHERE "RemoteVideoId" <> '00000000-0000-0000-0000-000000000000'
                  AND "State" = 0;
                """, cancellationToken);

            await ExecuteAsync(connection, """
                CREATE INDEX IF NOT EXISTS "IX_VideosToUpload_State_NextAttemptAtUtc"
                ON "VideosToUpload" ("State", "NextAttemptAtUtc");
                """, cancellationToken);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private static async Task<HashSet<string>> ReadColumnsAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{TableName}\");";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(cancellationToken))
            columns.Add(reader.GetString(1));
        return columns;
    }

    private static async Task AddColumnAsync(
        DbConnection connection,
        ISet<string> columns,
        string name,
        string definition,
        CancellationToken cancellationToken)
    {
        if (columns.Contains(name))
            return;

        await ExecuteAsync(
            connection,
            $"ALTER TABLE \"{TableName}\" ADD COLUMN \"{name}\" {definition};",
            cancellationToken);
        columns.Add(name);
    }

    private static async Task ExecuteAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
