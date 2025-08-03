# Job Lifecycle

Chronux manages the full lifecycle of a background job, from scheduling to execution, retries, and logging.

---

## 1️⃣ Trigger Fires

- `TriggerScheduler` runs every N seconds
- Calls `.GetNextOccurrence()` for all triggers
- If now ≥ scheduled time → enqueue job

---

## 2️⃣ Job Enqueued

- Uses `IChronuxStorageProvider.EnqueueJobAsync(...)`
- Includes:
    - JobId
    - Input object
    - Metadata (e.g., user, correlation id)
- Stored in Redis List / SQL Table

---

## 3️⃣ Job Dequeued

- Background dispatcher dequeues job from storage
- Deserializes input and metadata

---

## 4️⃣ Execution Pipeline

- Middleware executed in order:
    - Logging
    - Retry wrapper
    - Authorization
    - Tracing
- Calls `.ExecuteAsync(input, context, ct)`

---

## 5️⃣ Result Handling

- On `JobResult.Success`: logs stored via `AppendExecutionLogAsync`
- On `JobResult.Failure`:
    - Retry if policy allows
    - Otherwise → send to DLQ

---

## 🔄 Retry & DLQ Flow

1. Retry policy returns:
    - Retry delay (TimeSpan)
    - Retry count exceeded → DLQ
2. DLQ stored via `IDeadLetterStore.AddAsync(...)`
3. Metadata captured with:
    - Error message
    - Stack trace
    - Attempt count

---

## 🧠 Final Log

Execution logs include:
- Timestamp
- Result (Success/Failure)
- Duration
- Custom metadata
- Retry count
