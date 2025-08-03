namespace Chronux.Core.Jobs.Models;

public sealed class JobMetadata
{
    public required string Id { get; init; }
    public string? Description { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}