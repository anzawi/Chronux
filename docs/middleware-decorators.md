# Middleware & Decorators

Chronux provides full middleware support to hook into job execution.

---

## 🔄 Middleware Pipeline

```csharp
public interface IJobMiddleware
{
    ValueTask<JobResult> InvokeAsync(JobExecutionContext context, JobExecutionDelegate next);
}
```

Register via:

```csharp
builder.Services.AddChronux(...).UseMiddleware<LoggingMiddleware>();
```

Built as a chain:

```text
Middleware1 → Middleware2 → Job
```

## 🔁 Decorators

`IJobDecorator` allows wrapping execution logic outside the core job. Useful for:

- **Fallback**
- **Timeout**
- **Metrics**
- **Validation**

```csharp
public interface IJobDecorator
{
    ValueTask<JobResult> InvokeAsync(
        JobExecutionContext context,
        Func<CancellationToken, ValueTask<JobResult>> next,
        CancellationToken ct);
}
```

---

## 🧠 Use Cases

| Use Case        | Middleware | Decorator        |
|----------------|------------|------------------|
| Logging         | ✅         |                  |
| Retry           | ✅         | ✅ (advanced)     |
| Timeout         |            | ✅               |
| Circuit breaker |            | ✅               |
| Tracing         | ✅         |                  |

---

## 💡 Order of Execution

- **Decorators** wrap the **job**
- **Middleware** wraps the **decorators**


---

# 📁 `docs/sqlserver.md`

```markdown
# SQL Server Integration

Chronux supports SQL Server as a durable backend.

---

## ✅ Features

- Full `IChronuxStorageProvider` support
- Durable job + trigger definitions
- TriggerState, ExecutionLogs
- DLQ support
- Distributed locking via SQL

---

## 📦 Required Tables

- `Chronux_Jobs`
- `Chronux_Triggers`
- `Chronux_ExecutionLogs`
- `Chronux_DeadLetters`
- `Chronux_TriggerState`

DDL provided in the `schema.sql` file.

---

## 🛠 Integration

```csharp
options.UseSqlServerStorage(services, connectionString);
```

Supports:
- `UpsertJobAsync(...)`
- `LoadAllJobsAsync()`
- `DequeueJobAsync()`

---

## 🔐 SQL Locking

Implemented using:

```sql
SELECT ... WITH (UPDLOCK, ROWLOCK)
```

Avoids race conditions in clustered environments.

---

## 🚫 No EF Migrations

Tables must be created manually.
