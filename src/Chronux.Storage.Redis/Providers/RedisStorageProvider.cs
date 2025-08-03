using Chronux.Core.Enqueuing.Models;
using Chronux.Core.Serialization.Contracts;
using Chronux.Core.Storage.Contracts;
using Chronux.Core.Storage.Models;
using Chronux.Storage.Redis.Internal;
using StackExchange.Redis;

namespace Chronux.Storage.Redis.Providers;

public sealed class RedisStorageProvider(
    IConnectionMultiplexer redis,
    IChronuxSerializer serializer)
    : IChronuxStorageProvider
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task EnqueueJobAsync(string jobId, object input, EnqueueMetadata? metadata,
        CancellationToken ct = default)
    {
        var data = new EnqueuedJob
        {
            JobId = jobId,
            Input = input,
            Metadata = metadata
        };
        var payload = serializer.Serialize(data);

        await _db.ListRightPushAsync(RedisKeys.JobQueue(jobId), payload);
    }

    public async Task EnqueueJobAsync(EnqueuedJob job, CancellationToken ct = default)
    {
        var payload = serializer.Serialize(job);
        await _db.ListRightPushAsync(RedisKeys.JobQueue(job.JobId), payload);
    }

    public async Task<EnqueuedJob?> DequeueJobAsync(string jobId, CancellationToken ct = default)
    {
        var payload = await _db.ListLeftPopAsync(RedisKeys.JobQueue(jobId));
        if (payload.IsNullOrEmpty) return null;

        return serializer.Deserialize<EnqueuedJob>(payload!);
    }

    public async Task<TriggerState?> GetTriggerStateAsync(string jobId, CancellationToken ct = default)
    {
        var key = RedisKeys.TriggerState(jobId);
        var value = await _db.HashGetAsync(key, "state");
        if (value.IsNullOrEmpty) return null;

        return serializer.Deserialize<TriggerState>(value!);
    }

    public async Task SetTriggerStateAsync(string jobId, TriggerState state, CancellationToken ct = default)
    {
        var key = RedisKeys.TriggerState(jobId);
        var payload = serializer.Serialize(state);
        await _db.HashSetAsync(key, [new HashEntry("state", payload)]);
    }

    public async Task RemoveTriggerStateAsync(string jobId, CancellationToken ct = default)
    {
        var key = RedisKeys.TriggerState(jobId);
        await _db.KeyDeleteAsync(key);
    }

    public async Task AppendExecutionLogAsync(ExecutionLog log, CancellationToken ct = default)
    {
        var key = RedisKeys.ExecutionLogs(log.JobId);
        var payload = serializer.Serialize(log);
        var score = log.ExecutedAt.ToUnixTimeMilliseconds();

        await _db.SortedSetAddAsync(key, payload, score);
    }

    public async Task<IReadOnlyList<ExecutionLog>> QueryExecutionLogsAsync(string jobId, int take = 100, CancellationToken ct = default)
    {
        var key = RedisKeys.ExecutionLogs(jobId);
        var results = await _db.SortedSetRangeByRankAsync(key, -take, -1, Order.Descending);

        var list = new List<ExecutionLog>();
        foreach (var item in results)
        {
            if (!item.IsNullOrEmpty)
                list.Add(serializer.Deserialize<ExecutionLog>(item!)!);
        }

        return list;
    }


    public async Task<IReadOnlyList<ExecutionLog>> GetExecutionLogsAsync(string jobId, CancellationToken ct = default)
    {
        var key = RedisKeys.ExecutionLogs(jobId);
        var results = await _db.SortedSetRangeByScoreAsync(key, order: Order.Descending, take: 100);

        return (from item in results where !item.IsNullOrEmpty select serializer.Deserialize<ExecutionLog>(item!)!)
            .ToList();
    }

    public Task<IReadOnlyList<ExecutionLog>> GetAllExecutionLogsAsync(CancellationToken ct = default)
    {
        // This implementation only supports per-job logs (as designed)
        // To avoid performance issues, this will be empty unless changed in future
        return Task.FromResult<IReadOnlyList<ExecutionLog>>([]);
    }

    public async Task PurgeExecutionLogsOlderThanAsync(DateTimeOffset threshold, CancellationToken ct = default)
    {
        var allJobIds = await DiscoverAllJobIdsAsync();

        foreach (var jobId in allJobIds)
        {
            var key = RedisKeys.ExecutionLogs(jobId);
            var score = threshold.ToUnixTimeMilliseconds();
            await _db.SortedSetRemoveRangeByScoreAsync(key, double.NegativeInfinity, score);
        }
    }

    public async Task PurgeExecutionLogsOverLimitAsync(int maxCount, CancellationToken ct = default)
    {
        var allJobIds = await DiscoverAllJobIdsAsync();

        foreach (var jobId in allJobIds)
        {
            var key = RedisKeys.ExecutionLogs(jobId);
            var total = await _db.SortedSetLengthAsync(key);

            if (total > maxCount)
            {
                var excess = total - maxCount;
                await _db.SortedSetRemoveRangeByRankAsync(key, 0, excess - 1);
            }
        }
    }

    private Task<IReadOnlyList<string>> DiscoverAllJobIdsAsync()
    {
        // 🧠 In production you’d store jobIds in a Redis Set or scan a key registry.
        // For now: return dummy or inject from config
        return Task.FromResult<IReadOnlyList<string>>([]); // Not implemented in Redis-mode for now
    }
}