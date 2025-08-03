using Chronux.Core.Execution.Contracts;
using Microsoft.Data.SqlClient;

namespace Chronux.Storage.SqlServer.Clustering;

public sealed class SqlServerLockProvider(string connectionString, string nodeId)
    : IDistributedLockProvider
{
    public async ValueTask<IDisposable?> TryAcquireLockAsync(string key, TimeSpan timeout, CancellationToken ct)
    {
        const string sql = """
            MERGE Chronux_ClusterLocks AS target
            USING (SELECT @Key AS LockKey) AS source
            ON target.LockKey = source.LockKey
            WHEN MATCHED AND target.ExpiresAt < SYSUTCDATETIME()
                THEN UPDATE SET
                    AcquiredBy = @NodeId,
                    AcquiredAt = SYSUTCDATETIME(),
                    ExpiresAt = DATEADD(SECOND, @TtlSeconds, SYSUTCDATETIME())
            WHEN NOT MATCHED
                THEN INSERT (LockKey, AcquiredBy, AcquiredAt, ExpiresAt)
                VALUES (@Key, @NodeId, SYSUTCDATETIME(), DATEADD(SECOND, @TtlSeconds, SYSUTCDATETIME()));
        """;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Key", key);
        cmd.Parameters.AddWithValue("@NodeId", nodeId);
        cmd.Parameters.AddWithValue("@TtlSeconds", (int)timeout.TotalSeconds);

        var rows = await cmd.ExecuteNonQueryAsync(ct);

        return rows > 0 ? new Releaser(() => ReleaseAsync(key)) : null;
    }

    private async Task ReleaseAsync(string key)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Chronux_ClusterLocks WHERE LockKey = @Key";
        cmd.Parameters.AddWithValue("@Key", key);

        await cmd.ExecuteNonQueryAsync();
    }

    private sealed class Releaser(Func<Task> releaseAsync) : IDisposable
    {
        public void Dispose() => releaseAsync().GetAwaiter().GetResult();
    }
}
