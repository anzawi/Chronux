using Chronux.Core.Configuration.Models;
using Chronux.Core.Scheduling.Models;

namespace Chronux.Core.Configuration.Internal.Model;

internal sealed class ChronuxConfigContext
{
    public List<JobDefinition> Jobs { get; } = [];
    public List<Type> MiddlewareTypes { get; } = [];

    public void AddJob(JobDefinition job) => Jobs.Add(job);
    public void AddMiddleware(Type middlewareType) => MiddlewareTypes.Add(middlewareType);
    
    public ChronuxOptions Options { get; } = new();
}