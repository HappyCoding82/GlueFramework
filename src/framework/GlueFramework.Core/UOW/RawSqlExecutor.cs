using Dapper;
using GlueFramework.Core.Abstractions;
using System.Data;

namespace GlueFramework.Core.UOW
{
    public sealed class RawSqlExecutor : IRawSqlExecutor
    {
        public RawSqlExecutor(IDbConnection connection, IDbTransaction? transaction)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Transaction = transaction;
        }

        public IDbConnection Connection { get; }

        public IDbTransaction? Transaction { get; }

        public Task<int> ExecuteAsync(
            string sql,
            object? param = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            return Connection.ExecuteAsync(sql, param, Transaction, commandTimeout, commandType);
        }

        public Task<IEnumerable<T>> QueryAsync<T>(
            string sql,
            object? param = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            return Connection.QueryAsync<T>(sql, param, Transaction, commandTimeout, commandType);
        }

        public Task<T> QuerySingleAsync<T>(
            string sql,
            object? param = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            return Connection.QuerySingleAsync<T>(sql, param, Transaction, commandTimeout, commandType);
        }

        public Task<T?> QuerySingleOrDefaultAsync<T>(
            string sql,
            object? param = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            return Connection.QuerySingleOrDefaultAsync<T>(sql, param, Transaction, commandTimeout, commandType);
        }

        public Task<SqlMapper.GridReader> QueryMultipleAsync(
            string sql,
            object? param = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            return Connection.QueryMultipleAsync(sql, param, Transaction, commandTimeout, commandType);
        }
    }
}
