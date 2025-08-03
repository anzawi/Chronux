using Chronux.Core.Scheduling.Models;

namespace Chronux.Core.Scheduling.Internal.Contracts;

internal interface IJobRegistry
{
    JobDefinition? Get(string id);
    IEnumerable<JobDefinition> All { get; }
}