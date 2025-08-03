using Chronux.Core.Serialization.Contracts;
using Chronux.Core.Storage.Contracts;
using Chronux.Core.Storage.Models;
using Chronux.Storage.Redis.Internal;
using StackExchange.Redis;

namespace Chronux.Storage.Redis.Providers;

public sealed class RedisDeadLetterStore(
    IConnectionMultiplexer redis,
    IChronuxSerializer serializer)
    : IDeadLetterStore
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task AddAsync(DeadLetterItem item, CancellationToken ct = default)
    {
        var key = RedisKeys.DeadLetter(item.JobId);
        var payload = serializer.Serialize(item);

        await _db.HashSetAsync(key, [new HashEntry("item", payload)]);
        await _db.SetAddAsync("chronux:dlq:index", item.JobId);
    }

    public async Task<DeadLetterItem?> GetByIdAsync(string jobId, CancellationToken ct = default)
    {
        var key = RedisKeys.DeadLetter(jobId);
        var value = await _db.HashGetAsync(key, "item");

        return value.IsNullOrEmpty ? null : serializer.Deserialize<DeadLetterItem>(value!);
    }

    public async Task<IReadOnlyList<DeadLetterItem>> GetAllAsync(CancellationToken ct = default)
    {
        var jobIds = await _db.SetMembersAsync("chronux:dlq:index");

        var list = new List<DeadLetterItem>();
        foreach (var jobId in jobIds)
        {
            var item = await GetByIdAsync(jobId!, ct);
            if (item is not null)
                list.Add(item);
        }

        return list;
    }

    public async Task<DeadLetterItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var jobIds = await _db.SetMembersAsync("chronux:dlq:index");

        foreach (var jobId in jobIds)
        {
            var item = await GetByIdAsync(jobId!, ct);
            if (item is not null && item.Id == id)
                return item;
        }

        return null;
    }


    public async Task DeleteAsync(string jobId, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync(RedisKeys.DeadLetter(jobId));
        await _db.SetRemoveAsync("chronux:dlq:index", jobId);
    }
}