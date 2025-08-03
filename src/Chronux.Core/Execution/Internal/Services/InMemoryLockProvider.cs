using System.Collections.Concurrent;
using Chronux.Core.Execution.Contracts;

namespace Chronux.Core.Execution.Internal.Services;

internal sealed class InMemoryLockProvider : IDistributedLockProvider
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async ValueTask<IDisposable?> TryAcquireLockAsync(string key, TimeSpan timeout, CancellationToken ct)
    {
        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        var acquired = await sem.WaitAsync(timeout, ct);

        return acquired ? new Releaser(() => sem.Release()) : null;
    }

    private sealed class Releaser(Action release) : IDisposable
    {
        public void Dispose() => release();
    }
}