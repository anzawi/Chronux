using Chronux.Core.Serialization.Contracts;
using Chronux.Core.Storage.Contracts;
using Chronux.Core.Storage.Models;

namespace Chronux.Core.Storage.Services;

public sealed class InMemoryDeadLetterStore(IChronuxSerializer serializer) : IDeadLetterStore
{
    private readonly List<(string JobId, string Json, DeadLetterItem Meta)> _items = [];

    public Task AddAsync(DeadLetterItem item, CancellationToken ct)
    {
        var json = serializer.Serialize(item.Input!);
        _items.Add((item.JobId, json, item with { Input = null }));
        return Task.CompletedTask;
    }
    
    public Task GetAsync(DeadLetterItem item, CancellationToken ct)
    {
        var json = serializer.Serialize(item.Input!);
        _items.Add((item.JobId, json, item with { Input = null }));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DeadLetterItem>> GetAllAsync(CancellationToken ct)
    {
        var result = _items.Select(x =>
        {
            var input = serializer.Deserialize(x.Json, x.Meta.Input?.GetType() ?? typeof(object));
            return x.Meta with { Input = input };
        }).ToList();

        return Task.FromResult<IReadOnlyList<DeadLetterItem>>(result);
    }

    public Task<DeadLetterItem?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        lock (_items)
        {
            var match = _items.FirstOrDefault(x => x.Meta.Id == id);
            return Task.FromResult<DeadLetterItem?>(match.Meta);
        }
    }


    public Task DeleteAsync(string jobId, CancellationToken ct)
    {
        _items.RemoveAll(x => x.JobId == jobId);
        return Task.CompletedTask;
    }
}
