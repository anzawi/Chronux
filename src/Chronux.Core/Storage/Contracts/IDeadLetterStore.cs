using Chronux.Core.Storage.Models;

namespace Chronux.Core.Storage.Contracts;

public interface IDeadLetterStore
{
    Task AddAsync(DeadLetterItem item, CancellationToken ct);
    Task<IReadOnlyList<DeadLetterItem>> GetAllAsync(CancellationToken ct);
    Task<DeadLetterItem?> GetByIdAsync(Guid id, CancellationToken ct);
    Task DeleteAsync(string jobId, CancellationToken ct);
}