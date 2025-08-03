# Chronux

**Ultra-Advanced, Distributed, Extensible Job Scheduler for .NET**

Chronux is a production-grade background job and trigger orchestration library for .NET — offering the power of Quartz.NET with the developer ergonomics and distributed support of Hangfire Pro. Built for modern .NET 8/9+ applications with full support for pluggable storage, clustering, middleware, and retries.

---

## ✨ Features

- ✅ Clean, fluent job registration
- ⏰ Cron, delay, and interval triggers with timezone support
- 🔁 Retry policies, dead-letter queue, and fault-tolerance
- 🔒 Distributed locks (SQL / Redis Redlock)
- 🧩 Storage-agnostic architecture (SQL Server, Redis)
- 🧠 Misfire detection and recovery
- ⚙️ Extensible via middleware and decorators
- 🚀 Ready for clustering and horizontal scaling
- 🌐 Server Mode (coming soon)

---

## 📦 Storage Backends

Chronux supports pluggable persistence via `IChronuxStorageProvider`.

| Backend      | Status   | Notes                         |
|--------------|----------|-------------------------------|
| SQL Server   | ✅ Stable | Job + trigger persisted       |
| Redis        | ✅ Stable | Fast, ephemeral + Redlock     |
| In-Memory    | ✅ Built-in | Good for dev/test only       |

---

## 🚀 Getting Started

```csharp
var redis = ConnectionMultiplexer.Connect("localhost:6379");

builder.Services.AddChronux(cfg =>
{
    cfg.AddJob<SendWelcomeEmail>().WithTrigger(new CronTrigger("welcome", "0 * * * *"));
}, options =>
{
    options.SerializerInstance = new JsonChronuxSerializer();
    options.UseRedisStorage(builder.Services, redis, useClusterLocks: true, nodeId: "node-a");
});
```

Jobs must implement:

```csharp
public sealed class SendWelcomeEmail : IChronuxJob<WelcomeInput>
{
    public ValueTask<JobResult> ExecuteAsync(WelcomeInput input, JobExecutionContext context, CancellationToken ct)
    {
    // your logic here
    }
}
```

---

## 🛠 Configuration

Chronux is fully configurable via `ChronuxOptions`:

- `DefaultRetryPolicy`
- `SerializerInstance`
- `MisfireStrategy`
- `StorageProvider`, `DeadLetterStore`
- `DistributedLockProvider`
- See [`docs/configuration.md`](docs/configuration.md) for details

---

## 🧩 Extending Chronux

Chronux is modular and open for extension:

- `IChronuxJob<TInput>`
- `IJobMiddleware` for global interceptors
- `IDeadLetterStore`, `IChronuxStorageProvider`
- `IDistributedLockProvider` (e.g., Redlock, SqlLock)
- `IJobDecorator` for instrumentation, metrics, fallback, etc.

---

## Full docs
- See [`docs/`](docs/) for full documentation

## 📜 License

MIT © 2025 Mohammad Anzawi ([@anzawi](https://github.com/anzawi))
