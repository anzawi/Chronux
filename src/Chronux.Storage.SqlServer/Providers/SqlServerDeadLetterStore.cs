using Chronux.Core.Serialization.Contracts;
using Chronux.Core.Storage.Contracts;
using Chronux.Core.Storage.Models;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using Chronux.Core.Enqueuing.Models;

namespace Chronux.Storage.SqlServer.Providers;

public sealed class SqlServerDeadLetterStore(
    string connectionString,
    IChronuxSerializer serializer) : IDeadLetterStore
{
    public async Task AddAsync(DeadLetterItem item, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO Chronux_DeadLetters (
                Id, JobId, FailedAt, Input, ErrorMessage, Exception,
                RetryAttempt, MaxAttempts, TriggerId, InstanceId, Tags,
                CorrelationId, TriggerSource, UserId)
            VALUES (
                @Id, @JobId, @FailedAt, @Input, @ErrorMessage, @Exception,
                @RetryAttempt, @MaxAttempts, @TriggerId, @InstanceId, @Tags,
                @CorrelationId, @TriggerSource, @UserId)
        """;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        cmd.Parameters.AddWithValue("@Id", item.Id);
        cmd.Parameters.AddWithValue("@JobId", item.JobId);
        cmd.Parameters.AddWithValue("@FailedAt", item.FailedAt);
        cmd.Parameters.AddWithValue("@Input", SerializeNullable(item.Input) ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ErrorMessage", (object?)item.ErrorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Exception", (object?)item.Exception?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RetryAttempt", item.RetryAttempt);
        cmd.Parameters.AddWithValue("@MaxAttempts", item.MaxAttempts);
        cmd.Parameters.AddWithValue("@TriggerId", (object?)item.TriggerId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@InstanceId", (object?)item.InstanceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Tags", (object?)string.Join(',', item.Tags ?? []) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CorrelationId", (object?)item.Metadata?.CorrelationId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TriggerSource", (object?)item.Metadata?.TriggerSource ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UserId", (object?)item.Metadata?.UserId ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<DeadLetterItem>> GetAllAsync(CancellationToken ct)
    {
        const string sql = "SELECT TOP 500 * FROM Chronux_DeadLetters ORDER BY FailedAt DESC";

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new List<DeadLetterItem>();

        while (await reader.ReadAsync(ct))
        {
            result.Add(new DeadLetterItem
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                JobId = reader.GetString(reader.GetOrdinal("JobId")),
                FailedAt = reader.GetDateTimeOffset(reader.GetOrdinal("FailedAt")),
                Input = DeserializeNullable(reader["Input"] as string),
                ErrorMessage = reader["ErrorMessage"] as string,
                Exception = ParseException(reader["Exception"] as string),
                RetryAttempt = reader.GetInt32(reader.GetOrdinal("RetryAttempt")),
                MaxAttempts = reader.GetInt32(reader.GetOrdinal("MaxAttempts")),
                TriggerId = reader["TriggerId"] as string,
                InstanceId = reader["InstanceId"] as string,
                Tags = reader["Tags"]?.ToString()?.Split(','),
                Metadata = new EnqueueMetadata
                {
                    CorrelationId = reader["CorrelationId"] as string,
                    TriggerSource = reader["TriggerSource"] as string,
                    UserId = reader["UserId"] as string
                }
            });
        }

        return result;
    }


    public async Task<DeadLetterItem?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        const string sql = "SELECT * FROM Chronux_DeadLetters WHERE Id = @Id";

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new DeadLetterItem
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            JobId = reader.GetString(reader.GetOrdinal("JobId")),
            FailedAt = reader.GetDateTimeOffset(reader.GetOrdinal("FailedAt")),
            Input = DeserializeNullable(reader["Input"] as string),
            ErrorMessage = reader["ErrorMessage"] as string,
            Exception = ParseException(reader["Exception"] as string),
            RetryAttempt = reader.GetInt32(reader.GetOrdinal("RetryAttempt")),
            MaxAttempts = reader.GetInt32(reader.GetOrdinal("MaxAttempts")),
            TriggerId = reader["TriggerId"] as string,
            InstanceId = reader["InstanceId"] as string,
            Tags = reader["Tags"]?.ToString()?.Split(','),
            Metadata = new EnqueueMetadata
            {
                CorrelationId = reader["CorrelationId"] as string,
                TriggerSource = reader["TriggerSource"] as string,
                UserId = reader["UserId"] as string
            }
        };
    }

    public async Task DeleteAsync(string jobId, CancellationToken ct)
    {
        const string sql = "DELETE FROM Chronux_DeadLetters WHERE JobId = @JobId";

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@JobId", jobId);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static Exception? ParseException(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : new Exception(text);

    private string? SerializeNullable(object? value) =>
        value is null ? null : serializer.Serialize(value);

    private object? DeserializeNullable(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<object>(json); }
        catch { return null; }
    }
}
