using Chronux.Core.Configuration.Models;
using Chronux.Storage.SqlServer.Clustering;

namespace Chronux.Storage.SqlServer.Extensions;

public static class ClusteringExtensions
{
    public static ChronuxOptions UseSqlServerClusterLocks(this ChronuxOptions options, string connectionString, string? nodeId = null)
    {
        var actualNodeId = nodeId ?? Environment.MachineName;

        options.DistributedLockProvider = new SqlServerLockProvider(connectionString, actualNodeId);
        return options;
    }
}