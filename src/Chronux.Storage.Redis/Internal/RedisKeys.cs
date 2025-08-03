namespace Chronux.Storage.Redis.Internal;

internal static class RedisKeys
{
    // Job Queue Keys
    public static string JobQueue(string jobId) => $"chronux:q:{jobId}";

    // Trigger State Key (stored as a Redis Hash)
    public static string TriggerState(string jobId) => $"chronux:ts:{jobId}";

    // Execution Logs (stored as SortedSet with timestamps as scores)
    public static string ExecutionLogs(string jobId) => $"chronux:log:{jobId}";

    // ☠Dead Letter Queue (can be per-job or global)
    public static string DeadLetter(string? jobId = null) =>
        jobId is null ? $"chronux:dlq" : $"chronux:dlq:{jobId}";

    // Distributed Lock Key (if Redlock is enabled)
    public static string LockKey(string lockName) => $"chronux:lock:{lockName}";
}