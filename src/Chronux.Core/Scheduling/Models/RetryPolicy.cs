namespace Chronux.Core.Scheduling.Models;

public sealed class RetryPolicy
{
    public int MaxAttempts { get; private set; }
    public TimeSpan Delay { get; private set; }
    public RetryBackoff Strategy { get; private set; } = RetryBackoff.Constant;

    public RetryPolicy WithMaxAttempts(int attempts)
    {
        MaxAttempts = attempts;
        return this;
    }

    public RetryPolicy WithConstantBackoff(TimeSpan delay)
    {
        Delay = delay;
        Strategy = RetryBackoff.Constant;
        return this;
    }

    public RetryPolicy WithExponentialBackoff(TimeSpan delay)
    {
        Delay = delay;
        Strategy = RetryBackoff.Exponential;
        return this;
    }

    public void Validate()
    {
        if (MaxAttempts <= 0)
            throw new InvalidOperationException("RetryPolicy.MaxAttempts must be > 0");
        if (Delay <= TimeSpan.Zero)
            throw new InvalidOperationException("RetryPolicy.Delay must be set and > 0");
    }
}

public enum RetryBackoff
{
    None,
    Constant,
    Exponential
}