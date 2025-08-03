using Microsoft.Data.SqlClient;

namespace Chronux.Storage.SqlServer.JobDefinitions.Internal;

internal static class SqlTransactionScope
{
    public static async Task ExecuteAsync(
        string connectionString,
        Func<SqlConnection, SqlTransaction, Task> logic,
        CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

        try
        {
            await logic(conn, tx);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public static async Task<T> ExecuteAsync<T>(
        string connectionString,
        Func<SqlConnection, SqlTransaction, Task<T>> logic,
        CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

        try
        {
            var result = await logic(conn, tx);
            await tx.CommitAsync(ct);
            return result;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}