using Chronux.Core.Metrics.Models;

namespace Chronux.Core.Metrics.Contracts;

public interface IExecutionMetricsProvider
{
    Task<JobMetrics?> GetMetricsAsync(string jobId, CancellationToken ct = default);
    Task<IReadOnlyList<JobMetrics>> GetAllMetricsAsync(CancellationToken ct = default);
}