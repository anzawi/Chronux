using Chronux.Core.Diagnostics.Contracts;
using Chronux.Core.Diagnostics.Internal.Services;
using Chronux.Core.Execution.Contracts;
using Chronux.Core.Scheduling.Models;
using Chronux.Core.Serialization.Contracts;
using Chronux.Core.Storage.Contracts;

namespace Chronux.Core.Configuration.Models;

public class ChronuxOptions
{
    public bool AutoStartScheduler { get; set; } = true;
    public bool DisableWorker { get; set; } = false;
    public int? ThreadPoolMaxConcurrency { get; set; }

    public TimeSpan TriggerPollInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    public bool EnableDistributedLocking { get; set; } = false;
    public IDistributedLockProvider? DistributedLockProvider { get; set; }

    public RetryPolicy? RetryPolicy { get; set; }
    public TimeSpan? Timeout { get; set; }

    public string? StorageConnectionString { get; set; }
    public ChronuxStorageProviderType StorageProviderType { get; set; } = ChronuxStorageProviderType.InMemory;

    public bool EnableMisfireHandling { get; set; } = false;
    public bool EnableDiagnostics { get; set; } = false;
    public bool EnableDashboard { get; set; } = false;

    public string? InstanceId { get; set; }
    public string? SchedulerId { get; set; }

    public ChronuxSerializerType Serializer { get; set; } = ChronuxSerializerType.Json;
    public IChronuxSerializer? SerializerInstance { get; set; }
    public IChronuxDiagnostics Diagnostics { get; set; } = new EmptyDiagnostics();
    public IChronuxStorageProvider? StorageProvider { get; set; }
    public ChronuxRetentionOptions Retention { get; init; } = new();
    public IDeadLetterStore? DeadLetterStore { get; set; }

}