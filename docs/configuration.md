# Chronux Configuration

Chronux is highly configurable via the `ChronuxOptions` object passed during service registration. This document outlines all available options, their defaults, and usage.

---

## 🔧 ChronuxOptions

```csharp
builder.Services.AddChronux(cfg =>
{
    // Job registration
}, options =>
{
    options.DefaultRetryPolicy = RetryPolicy.Exponential(...);
    options.SerializerInstance = new JsonChronuxSerializer();
    options.StorageProvider = new SqlServerStorageProvider(...);
    options.DeadLetterStore = new SqlServerDeadLetterStore(...);
    options.DistributedLockProvider = new SqlDistributedLockProvider(...);
});
```

---

## ⚙️ Global Options

| Property                  | Description                                     | Default            |
|---------------------------|-------------------------------------------------|---------------------|
| `DefaultRetryPolicy`     | Controls how failed jobs are retried            | Exponential(3)      |
| `SerializerInstance`     | Required serializer for input/logs/etc.         | ❌ Required          |
| `StorageProvider`        | Implements job persistence                      | InMemory            |
| `DeadLetterStore`        | Handles failed jobs                             | InMemory            |
| `DistributedLockProvider`| Optional — for clustering safety                | `null`              |
| `MisfireStrategy`        | What happens when trigger misfires              | `MisfireStrategy.Ignore` |
| `LoadPersistedJobs`      | Load from persistent storage at startup         | `false`             |

---

## ⏲ Per-Job Options

| Option                   | Overridable? | Notes                         |
|--------------------------|--------------|-------------------------------|
| Retry policy             | ✅            | Can override per job          |
| Trigger misfire handling | ✅            | With `.WithMisfireStrategy()` |
| Input                    | ✅            | Must be serializable          |
| Execution metadata       | ✅            | Includes UserId, CorrelationId, etc. |

---

## 🧪 Example: Full Setup

```csharp
builder.Services.AddChronux(cfg =>
{
    cfg.AddJob<SendEmail>()
       .WithTrigger(new CronTrigger("email", "* * * * *"))
       .WithRetryPolicy(RetryPolicy.Linear(3));
}, options =>
{
    options.SerializerInstance = new JsonChronuxSerializer();
    options.UseSqlServerStorage(...);
    options.DefaultRetryPolicy = RetryPolicy.Exponential(5);
});
```

