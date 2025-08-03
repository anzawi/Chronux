using System.Data;
using System.Text.Json;
using Chronux.Core.Serialization.Contracts;
using Chronux.Core.Storage.Contracts;
using Chronux.Core.Storage.Models;
using Microsoft.Data.SqlClient;

namespace Chronux.Storage.SqlServer.Providers;

public sealed class SqlServerStorageProvider(
    string connectionString,
    IChronuxSerializer serializer) : IChronuxStorageProvider
{
    public async Task AppendExecutionLogAsync(ExecutionLog log, CancellationToken ct = default)
    {
        const string sql = """
                               INSERT INTO Chronux_ExecutionLogs (
                                   LogId, JobId, ExecutedAt, Success, Message, Exception,
                                   DurationMs, RetryAttempt, RetryCount, MaxAttemptsReached,
                                   TriggerId, InstanceId, Tags, CorrelationId, TriggerSource,
                                   UserId, Output)
                               VALUES (
                                   @LogId, @JobId, @ExecutedAt, @Success, @Message, @Exception,
                                   @DurationMs, @RetryAttempt, @RetryCount, @MaxAttemptsReached,
                                   @TriggerId, @InstanceId, @Tags, @CorrelationId, @TriggerSource,
                                   @UserId, @Output)
                           """;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        cmd.Parameters.AddWithValue("@LogId", Guid.NewGuid());
        cmd.Parameters.AddWithValue("@JobId", log.JobId);
        cmd.Parameters.AddWithValue("@ExecutedAt", log.ExecutedAt);
        cmd.Parameters.AddWithValue("@Success", log.Success);
        cmd.Parameters.AddWithValue("@Message", (object?)log.Message ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Exception", (object?)log.Exception?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DurationMs", (int)log.Duration.TotalMilliseconds);
        cmd.Parameters.AddWithValue("@RetryAttempt", (object?)log.RetryAttempt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RetryCount", (object?)log.RetryCount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@MaxAttemptsReached", (object?)log.MaxAttemptsReached ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TriggerId", (object?)log.TriggerId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@InstanceId", (object?)log.InstanceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Tags", (object?)string.Join(',', log.Tags ?? []) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CorrelationId", (object?)log.CorrelationId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TriggerSource", (object?)log.TriggerSource ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UserId", (object?)log.UserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Output", (object?)SerializeNullable(log.Output) ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<ExecutionLog>> GetExecutionLogsAsync(string jobId, CancellationToken ct = default)
    {
        const string sql = """
                               SELECT TOP 100 * FROM Chronux_ExecutionLogs
                               WHERE JobId = @JobId
                               ORDER BY ExecutedAt DESC
                           """;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@JobId", jobId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var results = new List<ExecutionLog>();

        while (await reader.ReadAsync(ct))
        {
            results.Add(new ExecutionLog
            {
                JobId = reader.GetString(reader.GetOrdinal("JobId")),
                ExecutedAt = reader.GetDateTimeOffset(reader.GetOrdinal("ExecutedAt")),
                Success = reader.GetBoolean(reader.GetOrdinal("Success")),
                Message = reader["Message"] as string,
                Exception = ParseException(reader["Exception"] as string),
                Duration = TimeSpan.FromMilliseconds(reader.GetInt32(reader.GetOrdinal("DurationMs"))),
                RetryAttempt = reader["RetryAttempt"] as int?,
                RetryCount = reader["RetryCount"] as int?,
                MaxAttemptsReached = reader["MaxAttemptsReached"] as bool?,
                TriggerId = reader["TriggerId"] as string,
                InstanceId = reader["InstanceId"] as string,
                Tags = reader["Tags"]?.ToString()?.Split(','),
                CorrelationId = reader["CorrelationId"] as string,
                TriggerSource = reader["TriggerSource"] as string,
                UserId = reader["UserId"] as string,
                Output = DeserializeNullable(reader["Output"] as string)
            });
        }

        return results;
    }

    private static Exception? ParseException(string? text)
    {
        return string.IsNullOrWhiteSpace(text) ? null : new Exception(text);
    }

    private string? SerializeNullable(object? value)
    {
        return value is null ? null : serializer.Serialize(value);
    }

    private object? DeserializeNullable(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<object>(json); // generic object fallback
        }
        catch
        {
            return null;
        }
    }

    public async Task<TriggerState?> GetTriggerStateAsync(string jobId, CancellationToken ct = default)
    {
        const string sql = """
                               SELECT * FROM Chronux_TriggerStates WHERE JobId = @JobId
                           """;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@JobId", jobId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new TriggerState
            {
                JobId = jobId,
                LastFiredAt = reader["LastFiredAt"] as DateTimeOffset?,
                NextDueAt = reader["NextDueAt"] as DateTimeOffset?,
                TriggerId = reader["TriggerId"] as string
            };
        }

        return null;
    }

    public async Task SetTriggerStateAsync(string jobId, TriggerState state, CancellationToken ct = default)
    {
        const string sql = """
                               MERGE Chronux_TriggerStates AS target
                               USING (SELECT @JobId AS JobId) AS source
                               ON target.JobId = source.JobId
                               WHEN MATCHED THEN
                                   UPDATE SET
                                       LastFiredAt = @LastFiredAt,
                                       NextDueAt = @NextDueAt,
                                       TriggerId = @TriggerId
                               WHEN NOT MATCHED THEN
                                   INSERT (JobId, LastFiredAt, NextDueAt, TriggerId)
                                   VALUES (@JobId, @LastFiredAt, @NextDueAt, @TriggerId);
                           """;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        cmd.Parameters.AddWithValue("@JobId", jobId);
        cmd.Parameters.AddWithValue("@LastFiredAt", (object?)state.LastFiredAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@NextDueAt", (object?)state.NextDueAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TriggerId", (object?)state.TriggerId ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RemoveTriggerStateAsync(string jobId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM Chronux_TriggerStates WHERE JobId = @JobId";

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@JobId", jobId);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task EnqueueJobAsync(EnqueuedJob job, CancellationToken ct = default)
    {
        const string sql = """
                               INSERT INTO Chronux_JobQueue (Id, JobId, Input, EnqueuedAt)
                               VALUES (@Id, @JobId, @Input, @EnqueuedAt)
                           """;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("@JobId", job.JobId);
        cmd.Parameters.AddWithValue("@Input", SerializeNullable(job.Input) ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@EnqueuedAt", job.EnqueuedAt);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<EnqueuedJob?> DequeueJobAsync(string jobId, CancellationToken ct = default)
    {
        const string sql = """
                               SELECT TOP 1 * FROM Chronux_JobQueue WHERE JobId = @JobId ORDER BY EnqueuedAt
                           """;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

        await using var select = conn.CreateCommand();
        select.Transaction = tx;
        select.CommandText = sql;
        select.Parameters.AddWithValue("@JobId", jobId);

        await using var reader = await select.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        var id = reader.GetGuid(reader.GetOrdinal("Id"));
        var input = DeserializeNullable(reader["Input"] as string);
        var enqueuedAt = reader.GetDateTimeOffset(reader.GetOrdinal("EnqueuedAt"));

        reader.Close();

        await using var delete = conn.CreateCommand();
        delete.Transaction = tx;
        delete.CommandText = "DELETE FROM Chronux_JobQueue WHERE Id = @Id";
        delete.Parameters.AddWithValue("@Id", id);
        await delete.ExecuteNonQueryAsync(ct);

        await tx.CommitAsync(ct);

        return new EnqueuedJob
        {
            JobId = jobId,
            Input = input,
            EnqueuedAt = enqueuedAt
        };
    }

    public async Task PurgeExecutionLogsOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct)
    {
        const string sql = """
                               DELETE FROM Chronux_ExecutionLogs
                               WHERE ExecutedAt < @Cutoff
                           """;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Cutoff", cutoff);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task PurgeExecutionLogsOverLimitAsync(int maxPerJob, CancellationToken ct)
    {
        const string sql = """
                               WITH OrderedLogs AS (
                                   SELECT *,
                                       ROW_NUMBER() OVER (PARTITION BY JobId ORDER BY ExecutedAt DESC) AS rn
                                   FROM Chronux_ExecutionLogs
                               )
                               DELETE FROM OrderedLogs WHERE rn > @MaxPerJob
                           """;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@MaxPerJob", maxPerJob);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<ExecutionLog>> GetAllExecutionLogsAsync(CancellationToken ct = default)
    {
        const string sql = """
                               SELECT TOP 1000 * FROM Chronux_ExecutionLogs
                               ORDER BY ExecutedAt DESC
                           """;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var results = new List<ExecutionLog>();

        while (await reader.ReadAsync(ct))
        {
            results.Add(new ExecutionLog
            {
                JobId = reader.GetString(reader.GetOrdinal("JobId")),
                ExecutedAt = reader.GetDateTimeOffset(reader.GetOrdinal("ExecutedAt")),
                Success = reader.GetBoolean(reader.GetOrdinal("Success")),
                Message = reader["Message"] as string,
                Exception = ParseException(reader["Exception"] as string),
                Duration = TimeSpan.FromMilliseconds(reader.GetInt32(reader.GetOrdinal("DurationMs"))),
                RetryAttempt = reader["RetryAttempt"] as int?,
                RetryCount = reader["RetryCount"] as int?,
                MaxAttemptsReached = reader["MaxAttemptsReached"] as bool?,
                TriggerId = reader["TriggerId"] as string,
                InstanceId = reader["InstanceId"] as string,
                Tags = reader["Tags"]?.ToString()?.Split(','),
                CorrelationId = reader["CorrelationId"] as string,
                TriggerSource = reader["TriggerSource"] as string,
                UserId = reader["UserId"] as string,
                Output = DeserializeNullable(reader["Output"] as string)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<ExecutionLog>> QueryExecutionLogsAsync(
        string jobId,
        int take = 100,
        CancellationToken ct = default)
    {
        const string sql = """
                               SELECT TOP (@Take) * FROM Chronux_ExecutionLogs
                               WHERE JobId = @JobId
                               ORDER BY ExecutedAt DESC
                           """;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@JobId", jobId);
        cmd.Parameters.AddWithValue("@Take", take);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var results = new List<ExecutionLog>();

        while (await reader.ReadAsync(ct))
        {
            results.Add(new ExecutionLog
            {
                JobId = reader.GetString(reader.GetOrdinal("JobId")),
                ExecutedAt = reader.GetDateTimeOffset(reader.GetOrdinal("ExecutedAt")),
                Success = reader.GetBoolean(reader.GetOrdinal("Success")),
                Message = reader["Message"] as string,
                Exception = ParseException(reader["Exception"] as string),
                Duration = TimeSpan.FromMilliseconds(reader.GetInt32(reader.GetOrdinal("DurationMs"))),
                RetryAttempt = reader["RetryAttempt"] as int?,
                RetryCount = reader["RetryCount"] as int?,
                MaxAttemptsReached = reader["MaxAttemptsReached"] as bool?,
                TriggerId = reader["TriggerId"] as string,
                InstanceId = reader["InstanceId"] as string,
                Tags = reader["Tags"]?.ToString()?.Split(','),
                CorrelationId = reader["CorrelationId"] as string,
                TriggerSource = reader["TriggerSource"] as string,
                UserId = reader["UserId"] as string,
                Output = DeserializeNullable(reader["Output"] as string)
            });
        }

        return results;
    }
}