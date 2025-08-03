using Chronux.Core.Configuration.Contracts;
using Chronux.Core.Configuration.Models;
using Chronux.Core.Storage.Contracts;
using Chronux.Core.Storage.Internal;
using Chronux.Core.Storage.providers;
using Chronux.Core.Storage.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Chronux.Core.Configuration.Extensions;


public static class StorageExtensions
{
    public static ChronuxOptions UseInMemoryStorage(this ChronuxOptions options)
    {
        options.StorageProvider = new InMemoryStorageProvider();
        return options;
    }

    public static ChronuxOptions UseStorage(this ChronuxOptions options, IChronuxStorageProvider provider)
    {
        options.StorageProvider = provider;
        return options;
    }
    
    public static IChronuxBuilder UseInMemoryStorage(this IChronuxBuilder builder)
    {
        builder.Services.AddSingleton<IChronuxStorageProvider, InMemoryStorageProvider>();
        builder.Services.AddSingleton<IJobQueueStore, InMemoryJobQueueStore>();
        builder.Services.AddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>();
        return builder;
    }
}

/*
🧩 2.5.D: Support for SQL Server, Redis, Oracle
   we can implement these later by simply:
   
   Creating SqlServerJobQueueStore, RedisJobQueueStore, etc.
   
   Registering them via UseSqlServerStorage(...), etc.
   
   Using serializer for input/output durability
   
   We've already planned:
   
   ✅ Chronux.Storage.SqlServer
   ✅ Chronux.Storage.Oracle (Maybe we cant now due the Oracle DB not installed)
   ✅ Chronux.Storage.Redis
   ✅ Chronux.Storage.Postgres
   */