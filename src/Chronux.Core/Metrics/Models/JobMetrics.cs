namespace Chronux.Core.Metrics.Models;

public sealed class JobMetrics
{
    public required string JobId { get; init; }

    public int TotalExecutions { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public int DeadLetterCount { get; init; }
    public int TotalRetryAttempts { get; init; }

    public double? AverageDurationMs { get; init; }
    public double SuccessRate => TotalExecutions == 0 ? 0 : (double)SuccessCount / TotalExecutions * 100;
}