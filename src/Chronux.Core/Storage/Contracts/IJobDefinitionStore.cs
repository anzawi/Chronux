using Chronux.Core.Scheduling.Models;

namespace Chronux.Core.Storage.Contracts;

public interface IJobDefinitionStore
{
    Task<IReadOnlyList<JobDefinition>> LoadAllAsync(CancellationToken ct = default);

    Task<JobDefinition?> GetAsync(string jobId, CancellationToken ct = default);

    Task UpsertAsync(JobDefinition job, CancellationToken ct = default);

    Task DeleteAsync(string jobId, CancellationToken ct = default);
}