namespace Chronux.Core.Validation.Models;

public sealed class JobValidationError
{
    public required string JobId { get; init; }
    public required string Message { get; init; }

    public override string ToString() => $"[{JobId}] {Message}";
}