namespace Chronux.Core.Jobs.Models;

public sealed class JobResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }
    public object? Data { get; init; }
    public IReadOnlyList<string>? NextJobIds { get; init; }

    public static JobResult Succeeded(params string[]? nextJobs) => new()
    {
        Success = true,
        NextJobIds = nextJobs?.ToList()
    };
    public static JobResult Succeeded(object? data, params string[]? nextJobs) => new()
    {
        Success = true,
        Data = data,
        NextJobIds = nextJobs?.ToList()
    };

    public static JobResult Failed(string? message = null, Exception? exception = null) => new()
    {
        Success = false,
        ErrorMessage = message,
        Exception = exception
    };
}