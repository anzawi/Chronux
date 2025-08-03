using System.Diagnostics;
using Chronux.Core.Execution.Contracts;
using StackExchange.Redis;

namespace Chronux.Storage.Redis.Clustering;

public sealed class RedisRedlockProvider(
    IReadOnlyList<IConnectionMultiplexer> nodes,
    string nodeId,
    TimeSpan retryDelay,
    int quorum = 1,
    TimeSpan? defaultTtl = null)
    : IDistributedLockProvider
{
    private readonly TimeSpan _ttl = defaultTtl ?? TimeSpan.FromSeconds(30);

    public async ValueTask<IDisposable?> TryAcquireLockAsync(string key, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var lockId = Guid.NewGuid().ToString("N");
        var redisKey = $"chronux:lock:{key}";
        var acquired = new List<(IDatabase db, IConnectionMultiplexer node)>();

        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
        {
            acquired.Clear();

            foreach (var node in nodes)
            {
                var db = node.GetDatabase();

                var success = await db.StringSetAsync(
                    key: redisKey,
                    value: lockId,
                    expiry: _ttl,
                    when: When.NotExists);

                if (success)
                    acquired.Add((db, node));
            }

            if (acquired.Count >= quorum)
            {
                return new RedisRedlockHandle(acquired, redisKey, lockId);
            }

            // Rollback partial locks
            foreach (var (db, _) in acquired)
            {
                await Release(db, redisKey, lockId);
            }

            await Task.Delay(retryDelay, cancellationToken);
        }

        return null;
    }

    private static async Task Release(IDatabase db, string key, string lockId)
    {
        const string lua = """
        if redis.call("get", KEYS[1]) == ARGV[1] then
            return redis.call("del", KEYS[1])
        else
            return 0
        end
        """;

        await db.ScriptEvaluateAsync(lua, [key], [lockId]);
    }

    private sealed class RedisRedlockHandle(
        IReadOnlyList<(IDatabase db, IConnectionMultiplexer node)> acquired,
        string key,
        string lockId)
        : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var (db, _) in acquired)
            {
                _ = Release(db, key, lockId); // fire and forget
            }
        }
    }
}