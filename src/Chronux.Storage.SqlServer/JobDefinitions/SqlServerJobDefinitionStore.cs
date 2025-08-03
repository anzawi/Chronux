using System.Data.Common;
using System.Text.Json;
using Chronux.Core.Scheduling.Models;
using Chronux.Core.Triggers.Builtin;
using Chronux.Core.Triggers.Contracts;
using Chronux.Core.Triggers.Models;
using Chronux.SqlServer.JobDefinitions;
using Chronux.Storage.SqlServer.JobDefinitions.Internal;
using Microsoft.Data.SqlClient;

namespace Chronux.Storage.SqlServer.JobDefinitions;

public sealed class SqlServerJobDefinitionStore(string connectionString) : IJobDefinitionStore
{
    public async Task<JobDefinition?> GetAsync(string jobId, CancellationToken ct = default)
    {
        var jobs = await LoadAllInternalAsync(jobIdFilter: jobId, ct);
        return jobs.FirstOrDefault();
    }

    public async Task<IReadOnlyList<JobDefinition>> LoadAllAsync(CancellationToken ct = default)
    {
        return await LoadAllInternalAsync(null, ct);
    }

    public async Task UpsertAsync(JobDefinition job, CancellationToken ct = default)
    {
        await SqlTransactionScope.ExecuteAsync(connectionString, async (conn, tx) =>
        {
            await UpsertJobAsync(conn, tx, job, ct);
            await UpsertTriggerAsync(conn, tx, job, ct);
        }, ct);
    }

    public async Task DeleteAsync(string jobId, CancellationToken ct = default)
    {
        const string deleteTriggers = "DELETE FROM Chronux_Triggers WHERE JobId = @JobId";
        const string deleteJobs = "DELETE FROM Chronux_Jobs WHERE JobId = @JobId";

        await SqlTransactionScope.ExecuteAsync(connectionString, async (conn, tx) =>
        {
            foreach (var cmdText in new[] { deleteTriggers, deleteJobs })
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = cmdText;
                cmd.Parameters.AddWithValue("@JobId", jobId);
                await cmd.ExecuteNonQueryAsync(ct);
            }
        }, ct);
    }


    private static async Task UpsertJobAsync(SqlConnection conn,  SqlTransaction tx, JobDefinition job, CancellationToken ct)
    {
        const string sql = """
            MERGE Chronux_Jobs AS target
            USING (SELECT @JobId AS JobId) AS source
            ON target.JobId = source.JobId
            WHEN MATCHED THEN
                UPDATE SET
                    HandlerType = @HandlerType,
                    ContextType = @ContextType,
                    Description = @Description,
                    Tags = @Tags,
                    Metadata = @Metadata,
                    TimeoutSec = @Timeout,
                    UseDistributedLock = @UseLock,
                    LockKey = @LockKey
            WHEN NOT MATCHED THEN
                INSERT (JobId, HandlerType, ContextType, Description, Tags, Metadata, TimeoutSec, UseDistributedLock, LockKey)
                VALUES (@JobId, @HandlerType, @ContextType, @Description, @Tags, @Metadata, @Timeout, @UseLock, @LockKey);
        """;

        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;

        cmd.Parameters.AddWithValue("@JobId", job.Id);
        cmd.Parameters.AddWithValue("@HandlerType", job.HandlerType.FullName!);
        cmd.Parameters.AddWithValue("@ContextType", job.ContextType.FullName!);
        cmd.Parameters.AddWithValue("@Description", (object?)job.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Tags", (object?)string.Join(',', job.Tags) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Metadata", (object?)JsonSerializer.Serialize(job.Metadata) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Timeout", (object?)job.Timeout?.TotalSeconds ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UseLock", job.UseDistributedLock);
        cmd.Parameters.AddWithValue("@LockKey", (object?)job.LockKey ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task UpsertTriggerAsync(SqlConnection conn,  SqlTransaction tx, JobDefinition job, CancellationToken ct)
    {
        if (job.Trigger is null)
        {
            await using var delete = conn.CreateCommand();
            delete.Transaction = tx;
            delete.CommandText = "DELETE FROM Chronux_Triggers WHERE JobId = @JobId";
            delete.Parameters.AddWithValue("@JobId", job.Id);
            await delete.ExecuteNonQueryAsync(ct);
            return;
        }

        const string sql = """
            MERGE Chronux_Triggers AS target
            USING (SELECT @JobId AS JobId) AS source
            ON target.JobId = source.JobId
            WHEN MATCHED THEN
                UPDATE SET
                    TriggerId = @TriggerId,
                    Type = @Type,
                    Expression = @Expression,
                    IntervalSec = @IntervalSec,
                    DelaySec = @DelaySec,
                    TimeZone = @TimeZone,
                    EnableMisfire = @EnableMisfire
            WHEN NOT MATCHED THEN
                INSERT (JobId, TriggerId, Type, Expression, IntervalSec, DelaySec, TimeZone, EnableMisfire)
                VALUES (@JobId, @TriggerId, @Type, @Expression, @IntervalSec, @DelaySec, @TimeZone, @EnableMisfire);
        """;

        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;

        cmd.Parameters.AddWithValue("@JobId", job.Id);
        cmd.Parameters.AddWithValue("@TriggerId", job.Trigger.Id);
        cmd.Parameters.AddWithValue("@Type", job.Trigger.Type.ToString());
        cmd.Parameters.AddWithValue("@EnableMisfire", job.EnableMisfireHandling ?? false);
        cmd.Parameters.AddWithValue("@TimeZone", (object?)job.Trigger.TimeZone?.Id ?? DBNull.Value);

        switch (job.Trigger.Type)
        {
            case TriggerType.Cron:
                cmd.Parameters.AddWithValue("@Expression", (job.Trigger as CronTrigger)?.Expression ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@IntervalSec", DBNull.Value);
                cmd.Parameters.AddWithValue("@DelaySec", DBNull.Value);
                break;
            case TriggerType.Interval:
                cmd.Parameters.AddWithValue("@IntervalSec", GetInterval(job.Trigger));
                cmd.Parameters.AddWithValue("@Expression", DBNull.Value);
                cmd.Parameters.AddWithValue("@DelaySec", DBNull.Value);
                break;
            case TriggerType.Delay:
                cmd.Parameters.AddWithValue("@DelaySec", GetDelay(job.Trigger));
                cmd.Parameters.AddWithValue("@Expression", DBNull.Value);
                cmd.Parameters.AddWithValue("@IntervalSec", DBNull.Value);
                break;
            default:
                throw new NotSupportedException($"Trigger type {job.Trigger.Type} is not supported for SQL storage.");
        }

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<IReadOnlyList<JobDefinition>> LoadAllInternalAsync(string? jobIdFilter, CancellationToken ct)
    {
        var jobs = new Dictionary<string, JobDefinition>();

        const string jobSql = "SELECT * FROM Chronux_Jobs";
        const string where = " WHERE JobId = @JobId";

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var jobCmd = conn.CreateCommand();
        jobCmd.CommandText = jobIdFilter is null ? jobSql : jobSql + where;
        if (jobIdFilter is not null)
            jobCmd.Parameters.AddWithValue("@JobId", jobIdFilter);

        await using var reader = await jobCmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var jobId = reader.GetString(reader.GetOrdinal("JobId"));

            jobs[jobId] = new JobDefinition
            {
                Id = jobId,
                HandlerType = Type.GetType(reader.GetString(reader.GetOrdinal("HandlerType")))!,
                ContextType = Type.GetType(reader.GetString(reader.GetOrdinal("ContextType")))!,
                Description = reader["Description"] as string,
                Tags = reader["Tags"]?.ToString()?.Split(',').ToList() ?? [],
                Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(reader["Metadata"] as string ?? "{}"),
                Timeout = reader["TimeoutSec"] as int? is int s ? TimeSpan.FromSeconds(s) : null,
                UseDistributedLock = reader.GetBoolean(reader.GetOrdinal("UseDistributedLock")),
                LockKey = reader["LockKey"] as string
            };
        }

        reader.Close();

        if (jobs.Count == 0) return [];

        const string triggerSql = "SELECT * FROM Chronux_Triggers";
        await using var triggerCmd = conn.CreateCommand();
        triggerCmd.CommandText = jobIdFilter is null ? triggerSql : triggerSql + where;
        if (jobIdFilter is not null)
            triggerCmd.Parameters.AddWithValue("@JobId", jobIdFilter);

        await using var triggers = await triggerCmd.ExecuteReaderAsync(ct);
        while (await triggers.ReadAsync(ct))
        {
            var jobId = triggers.GetString(triggers.GetOrdinal("JobId"));
            if (!jobs.TryGetValue(jobId, out var job)) continue;

            var typeStr = triggers.GetString(triggers.GetOrdinal("Type"));
            var type = Enum.Parse<TriggerType>(typeStr);
            var triggerId = triggers.GetString(triggers.GetOrdinal("TriggerId"));
            var tz = triggers["TimeZone"] as string;

            job.Trigger = type switch
            {
                TriggerType.Cron => new  CronTrigger(
                    expression: triggers["Expression"] as string ?? "* * * * *",
                    timeZone: string.IsNullOrWhiteSpace(tz) ? null : TimeZoneInfo.FindSystemTimeZoneById(tz),
                    id: triggerId),

                TriggerType.Interval => new IntervalTrigger(
                    id: triggerId,
                    interval: TimeSpan.FromSeconds((int)triggers["IntervalSec"])),

                TriggerType.Delay => new DelayTrigger(
                    id: triggerId,
                    delay: TimeSpan.FromSeconds((int)triggers["DelaySec"])),

                _ => throw new NotSupportedException($"Trigger type {type} not supported")
            };

            job.EnableMisfireHandling = (bool)triggers["EnableMisfire"];
        }

        return jobs.Values.ToList();
    }

    private static int GetInterval(ITrigger trigger) =>
        trigger is IntervalTrigger i ? (int)i.Interval.TotalSeconds : throw new();

    private static int GetDelay(ITrigger trigger) =>
        trigger is DelayTrigger d ? (int)d.Delay.TotalSeconds : throw new();
}