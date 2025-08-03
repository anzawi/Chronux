using Chronux.Core.Scheduling.Internal.Contracts;
using Chronux.Core.Scheduling.Models;

namespace Chronux.Core.Scheduling.Internal.Services;

internal sealed class JobRegistry : IJobRegistry
{
    private readonly Dictionary<string, JobDefinition> _jobs = new();

    public void Register(JobDefinition job)
    {
        if (!_jobs.TryAdd(job.Id, job))
            throw new InvalidOperationException($"Job with ID '{job.Id}' already exists.");
    }

    public JobDefinition? Get(string id) => _jobs.GetValueOrDefault(id);

    public IEnumerable<JobDefinition> All => _jobs.Values;
}