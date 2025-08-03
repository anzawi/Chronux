# Extending Chronux

Chronux is built with extensibility in mind.

---

## 🔧 Extensible Interfaces

| Interface                  | Purpose                              |
|----------------------------|--------------------------------------|
| `IChronuxJob<T>`           | Main job logic                       |
| `ITrigger`                 | Custom scheduling logic              |
| `IChronuxStorageProvider`  | Plug storage (e.g., Mongo, Azure)    |
| `IDeadLetterStore`         | Plug DLQ                             |
| `IJobMiddleware`           | Global cross-cutting logic           |
| `IJobDecorator`            | Inline job wrapping / instrumentation|
| `IDistributedLockProvider` | Locking strategy                     |
| `IChronuxSerializer`       | Binary/text serializer               |

---

## 🧱 Custom Trigger Example

```csharp
public class AlwaysNowTrigger : ITrigger
{
    public string Id => "immediate";
    public TriggerType Type => TriggerType.Custom;
    public TimeZoneInfo? TimeZone => null;

    public DateTimeOffset? GetNextOccurrence(DateTimeOffset after) => DateTimeOffset.UtcNow;
}
```