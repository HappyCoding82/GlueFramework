using Dapper;
using System.Data;

namespace GlueFramework.Core.Abstractions
{
    public interface IRawSqlExecutor
    {
        IDbConnection Connection { get; }

        IDbTransaction? Transaction { get; }

        Task<int> ExecuteAsync(
            string sql,
            object? param = null,
            int? commandTimeout = null,
            CommandType? commandType = null);

        Task<IEnumerable<T>> QueryAsync<T>(
            string sql,
            object? param = null,
            int? commandTimeout = null,
            CommandType? commandType = null);

        Task<T> QuerySingleAsync<T>(
            string sql,
            object? param = null,
            int? commandTimeout = null,
            CommandType? commandType = null);

        Task<T?> QuerySingleOrDefaultAsync<T>(
            string sql,
            object? param = null,
            int? commandTimeout = null,
            CommandType? commandType = null);

        Task<SqlMapper.GridReader> QueryMultipleAsync(
            string sql,
            object? param = null,
            int? commandTimeout = null,
            CommandType? commandType = null);
    }
}
