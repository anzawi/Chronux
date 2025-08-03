using Chronux.Core.Triggers.Contracts;

namespace Chronux.Core.Scheduling.Models;

public sealed class JobDefinition
{
    public string Id { get; set; } = null!;
    public Type HandlerType { get; set; } = null!;
    public Type ContextType { get; set; } = null!;
    public object? Input { get; set; } 
    public ITrigger? Trigger { get; set; }
    public RetryPolicy? Retry { get; set; }
    public TimeSpan? Timeout { get; set; }
    public bool UseDistributedLock { get; set; }
    public string? LockKey { get; set; }
    public bool? EnableMisfireHandling { get; set; }

    public string? Description { get; set; }

    public List<string>? OnSuccess { get; set; }
    public List<string>? OnFailure { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }
    public List<string> Tags { get; set; } = [];
    
    public List<string>? OnSuccessChain { get; set; }
    public List<string>? OnFailureChain { get; set; }
}