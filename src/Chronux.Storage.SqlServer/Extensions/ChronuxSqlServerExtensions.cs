using Chronux.Core.Configuration.Models;
using Chronux.Core.Serialization.Contracts;
using Chronux.SqlServer.JobDefinitions;
using Chronux.Storage.SqlServer.JobDefinitions;
using Chronux.Storage.SqlServer.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Chronux.Storage.SqlServer.Extensions;

public static class ChronuxSqlServerExtensions
{
    public static ChronuxOptions UseSqlServerStorage(
        this ChronuxOptions options,
        IServiceCollection services,
        string connectionString,
        bool useClusterLocks = true,
        string? nodeId = null)
    {
        options.StorageProvider = new SqlServerStorageProvider(
            connectionString,
            EnsureSerializer(options));

        options.DeadLetterStore = new SqlServerDeadLetterStore(
            connectionString,
            EnsureSerializer(options));

        if (useClusterLocks)
            options.UseSqlServerClusterLocks(connectionString, nodeId);
        
        services.AddSingleton<IJobDefinitionStore>(
            new SqlServerJobDefinitionStore(connectionString));
        return options;
    }
    

    private static IChronuxSerializer EnsureSerializer(ChronuxOptions options)
    {
        return options.SerializerInstance
               ?? throw new InvalidOperationException("A serializer must be configured before calling UseSqlServerStorage.");
    }
}
