using Chronux.Core.Configuration.Internal.Model;
using Chronux.Core.Scheduling.Contracts;
using Chronux.Core.Scheduling.Models;
using Chronux.Core.Triggers.Contracts;

namespace Chronux.Core.Scheduling.Internal.Services;

internal sealed class JobBuilder<TContext> : IJobBuilder
{
    private readonly JobDefinition _definition;

    private readonly ChronuxConfigContext _configContext;

    public JobBuilder(string jobId,
        Type handlerType,
        ChronuxConfigContext configContext)
    {
        _configContext = configContext;
        _definition = new JobDefinition
        {
            Id = jobId,
            HandlerType = handlerType,
            ContextType = typeof(TContext)
        };

        configContext.AddJob(_definition);
    }

    public IJobBuilder WithTrigger(ITrigger trigger)
    {
        _definition.Trigger = trigger;
        return this;
    }

    public IJobBuilder WithRetryPolicy(RetryPolicy policy)
    {
        _definition.Retry = policy;
        return this;
    }
    
    public IJobBuilder WithTimeout(TimeSpan timeout)
    {
        _definition.Timeout = timeout;
        return this;
    }

    public IJobBuilder WithRetryPolicy(Func<RetryPolicy, RetryPolicy> configure)
    {
        var policy = configure(new RetryPolicy());
        return WithRetryPolicy(policy);
    }

    public IJobBuilder WithDistributedLock(string? key = null)
    {
        _definition.UseDistributedLock = true;
        _definition.LockKey = key;
        return this;
    }

    public IJobBuilder WithChainedJobs(IEnumerable<string>? onSuccess = null, IEnumerable<string>? onFailure = null)
    {
        _definition.OnSuccess = onSuccess?.ToList();
        _definition.OnFailure = onFailure?.ToList();
        return this;
    }

    public IJobBuilder WithDescription(string? desc)
    {
        _definition.Description = desc;
        return this;
    }

    public IJobBuilder WithMetadata(string key, object value)
    {
        _definition.Metadata ??= new Dictionary<string, object>();
        _definition.Metadata[key] = value;
        return this;
    }

    public IJobBuilder WithInput<T>(T input)
    {
        if (input is not TContext typed)
            throw new InvalidOperationException("Input type mismatch for job context");

        _definition.Input = typed;
        return this;
    }
    
    public IJobBuilder WithTag(string tag)
    {
        _definition.Tags.Add(tag);
        return this;
    }

    public IJobBuilder WithTags(IEnumerable<string> tags)
    {
        _definition.Tags.AddRange(tags);
        return this;
    }

    public IJobBuilder WithMisfireHandling()
    {
        _definition.EnableMisfireHandling = true;
        return this;
    }

    public void Register()
    {
        if (_definition.Retry is null && _configContext.Options.RetryPolicy is { } fallback)
        {
            _definition.Retry = fallback;
        }

        _definition.EnableMisfireHandling ??= _configContext.Options.EnableMisfireHandling;

        _configContext.AddJob(_definition);
    }
}