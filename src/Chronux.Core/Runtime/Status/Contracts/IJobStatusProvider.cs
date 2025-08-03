using Chronux.Core.Runtime.Status.Models;

namespace Chronux.Core.Runtime.Status.Contracts;

public interface IJobStatusProvider
{
    Task<JobStatus?> GetStatusAsync(string jobId, CancellationToken ct = default);
    Task<IReadOnlyList<JobStatus>> GetAllStatusesAsync(CancellationToken ct = default);
}