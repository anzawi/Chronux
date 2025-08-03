using Chronux.Core.Configuration.Models;
using Chronux.Core.Serialization.Contracts;
using Chronux.Storage.Redis.Clustering;
using Chronux.Storage.Redis.Providers;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Chronux.Storage.Redis.Extensions;


public static class ChronuxRedisExtensions
{
    public static ChronuxOptions UseRedisStorage(
        this ChronuxOptions options,
        IServiceCollection services,
        IConnectionMultiplexer connection,
        bool useClusterLocks = true,
        string? nodeId = null)
    {
        var serializer = EnsureSerializer(options);

        options.StorageProvider = new RedisStorageProvider(connection, serializer);
        options.DeadLetterStore = new RedisDeadLetterStore(connection, serializer);

        if (useClusterLocks)
        {
            options.UseRedisClusterLocks([connection], nodeId ?? "default");
        }

        return options;
    }

    public static ChronuxOptions UseRedisClusterLocks(
        this ChronuxOptions options,
        IConnectionMultiplexer connection,
        string nodeId,
        TimeSpan? ttl = null,
        TimeSpan? retryDelay = null)
    {
        return options.UseRedisClusterLocks(
            [connection],
            nodeId,
            ttl ?? TimeSpan.FromSeconds(30),
            retryDelay ?? TimeSpan.FromMilliseconds(150),
            quorum: 1
        );
    }

    public static ChronuxOptions UseRedisClusterLocks(
        this ChronuxOptions options,
        IReadOnlyList<IConnectionMultiplexer> nodes,
        string nodeId,
        TimeSpan? ttl = null,
        TimeSpan? retryDelay = null,
        int quorum = 2)
    {
        var provider = new RedisRedlockProvider(
            nodes,
            nodeId,
            retryDelay ?? TimeSpan.FromMilliseconds(150),
            quorum,
            ttl ?? TimeSpan.FromSeconds(30)
        );

        options.DistributedLockProvider = provider;
        return options;
    }

    private static IChronuxSerializer EnsureSerializer(ChronuxOptions options)
    {
        return options.SerializerInstance
            ?? throw new InvalidOperationException("A serializer must be configured before calling UseRedisStorage.");
    }
}