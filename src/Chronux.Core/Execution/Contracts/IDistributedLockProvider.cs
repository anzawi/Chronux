namespace Chronux.Core.Execution.Contracts;

public interface IDistributedLockProvider
{
    /// <summary> Attempts to acquire a lock for a given key. </summary>
    ValueTask<IDisposable?> TryAcquireLockAsync(string key, TimeSpan timeout, CancellationToken cancellationToken);
}