namespace Chronux.Core.Configuration.Models;

public sealed class ChronuxRetentionOptions
{
    public TimeSpan? MaxAge { get; init; } = TimeSpan.FromDays(30);
    public int? MaxCountPerJob { get; init; } = 10_000;
}