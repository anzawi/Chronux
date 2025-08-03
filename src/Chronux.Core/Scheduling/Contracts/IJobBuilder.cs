using Chronux.Core.Scheduling.Models;
using Chronux.Core.Triggers.Contracts;

namespace Chronux.Core.Scheduling.Contracts;


public interface IJobBuilder
{
    IJobBuilder WithTrigger(ITrigger trigger);
    IJobBuilder WithRetryPolicy(RetryPolicy policy);
    IJobBuilder WithRetryPolicy(Func<RetryPolicy, RetryPolicy> configure);
    IJobBuilder WithTimeout(TimeSpan timeout);
    IJobBuilder WithDistributedLock(string? lockKey = null);
    IJobBuilder WithChainedJobs(IEnumerable<string>? onSuccess = null, IEnumerable<string>? onFailure = null);
    IJobBuilder WithDescription(string? description);
    IJobBuilder WithMetadata(string key, object value);
    IJobBuilder WithInput<TContext>(TContext input);
    IJobBuilder WithTag(string tag);
    IJobBuilder WithTags(IEnumerable<string> tags);
    IJobBuilder WithMisfireHandling();


    void Register();
}