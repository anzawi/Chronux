# Chronux Architecture

Chronux is a high-performance, extensible background job orchestration engine for .NET. It cleanly separates concerns like job registration, scheduling, execution, persistence, retry, logging, and clustering.

This document provides a deep-dive into Chronux's internal architecture, its extensibility model, and runtime behavior.

---

## 🧱 Core Components

```
Application Startup
|
├── IChronuxBuilder.AddJob<T>()
│
├── IJobRegistry
│   └── Stores all registered jobs (in-memory)
│
├── ITriggerScheduler (HostedService)
│   └── Evaluates triggers and enqueues jobs
│
├── JobExecutionPipeline
│   └── Invokes middleware → executes job → stores logs
│
├── IChronuxStorageProvider
│   └── Queue, TriggerState, Logs, EnqueueMetadata
│
├── IDeadLetterStore
│   └── Stores failed jobs after retries
│
└── IDistributedLockProvider (optional)
    └── Clustering and safe trigger execution
```


---

## 🔁 Job & Trigger Separation

Chronux separates **what runs (Job)** from **when it runs (Trigger)**.

### Example

```csharp
builder.Services.AddChronux(cfg =>
{
    cfg.AddJob<SendReport>()
    .WithTrigger(new CronTrigger("report-daily", "0 9 * * *", timeZone: TimeZoneInfo.Local));
});
```

- `SendReport` implements `IChronuxJob<T>`
- The job itself is **stateless**, while the trigger has scheduling logic
- Multiple triggers for same job: ✅ Supported

---

## 🚦 TriggerScheduler

Runs as a background service. On each tick:

1. Reads all triggers
2. Evaluates `GetNextOccurrence(...)`
3. If due:
    - Checks `TriggerState`
    - Acquires distributed lock (if enabled)
    - Enqueues the job using `IChronuxStorageProvider`

---

## 🛠 Interfaces

### `IChronuxJob<T>`

```csharp
public interface IChronuxJob<in T>
{
    ValueTask<JobResult> ExecuteAsync(T input, JobExecutionContext context, CancellationToken ct);
}
```

### `ITrigger`

```csharp
public interface ITrigger
{
    string Id { get; }
    TriggerType Type { get; }
    TimeZoneInfo? TimeZone { get; }
    DateTimeOffset? GetNextOccurrence(DateTimeOffset after);
}
```

Types include:
- `CronTrigger`
- `IntervalTrigger`
- `DelayTrigger`
- Custom triggers are supported

---

## 🔁 Job Execution Pipeline

When a job is dequeued:

1. `JobExecutionPipeline` builds a delegate chain from registered `IJobMiddleware`
2. Each middleware calls `next()`
3. The actual job handler runs
4. Result is logged via `AppendExecutionLogAsync`
5. If failed:
    - Retry policy applied
    - May go to DLQ (`IDeadLetterStore`)

---

## 📦 Storage Contracts

Chronux supports pluggable persistence:

### `IChronuxStorageProvider`
- Enqueue / Dequeue
- Execution logs (append, query)
- TriggerState (set/get)
- Optional: metrics, status

### `IDeadLetterStore`
- Add failed job
- Query all
- Get by jobId or itemId

### `IDistributedLockProvider`
- Used by TriggerScheduler to prevent duplicate execution across nodes
- Redis Redlock or SQL-based

---

## 🧩 Pluggable Implementations

Chronux Core defines all contracts. Providers exist for:

| Component          | In-Memory       | SQL Server        | Redis             |
|--------------------|------------------|--------------------|--------------------|
| Job queueing       | ✅               | ✅                 | ✅ (List)          |
| Trigger state      | ✅               | ✅                 | ✅ (Hash)          |
| Logs               | ✅               | ✅                 | ✅ (SortedSet)     |
| DLQ                | ✅               | ✅                 | ✅ (Hash+Set)      |
| Locking            | ❌               | ✅ (SQL)           | ✅ (Redlock)       |

---

## 🔐 Misfire Handling

Triggers may misfire if:
- The node was down
- The trigger ran late due to resource exhaustion

Chronux allows:
- **Ignore misfire**
- **Fire immediately**
- Configurable globally and per trigger

---

## 🧠 Design Principles

- **Single responsibility**: jobs, triggers, schedulers, and persistence are separated
- **Pluggable**: you can swap out Redis with SQL Server, or create your own storage
- **Safe & predictable**: no surprises with retries, logs, or state
- **Middleware-powered**: for observability and cross-cutting concerns
- **Cluster-aware**: Redlock and SQL locking support horizontal scaling

---

## ✅ Summary

Chronux’s architecture is:
- Minimal where it should be (e.g., in-memory mode)
- Powerful where it matters (DLQ, logs, retry, clustering)
- Extensible from day one

Next → see [`configuration.md`](./configuration.md) to learn about all options available.
