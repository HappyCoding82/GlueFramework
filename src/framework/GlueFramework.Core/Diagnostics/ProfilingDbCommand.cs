using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GlueFramework.Core.Diagnostics
{
    public sealed class ProfilingDbCommand : DbCommand
    {
        private readonly DbCommand _inner;
        private readonly IOptions<SlowSqlOptions> _options;
        private readonly ILogger _logger;

        public ProfilingDbCommand(DbCommand inner, IOptions<SlowSqlOptions> options, ILogger logger)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override string CommandText
        {
            get => _inner.CommandText;
            set => _inner.CommandText = value;
        }

        public override int CommandTimeout
        {
            get => _inner.CommandTimeout;
            set => _inner.CommandTimeout = value;
        }

        public override CommandType CommandType
        {
            get => _inner.CommandType;
            set => _inner.CommandType = value;
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get => _inner.UpdatedRowSource;
            set => _inner.UpdatedRowSource = value;
        }

        protected override DbConnection? DbConnection
        {
            get => _inner.Connection;
            set => _inner.Connection = value;
        }

        protected override DbParameterCollection DbParameterCollection => _inner.Parameters;

        protected override DbTransaction? DbTransaction
        {
            get => _inner.Transaction;
            set => _inner.Transaction = value;
        }

        public override bool DesignTimeVisible
        {
            get => _inner.DesignTimeVisible;
            set => _inner.DesignTimeVisible = value;
        }

        public override void Cancel() => _inner.Cancel();

        protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return ExecuteWithProfiling(() => _inner.ExecuteReader(behavior));
        }

        public override int ExecuteNonQuery()
        {
            return ExecuteWithProfiling(_inner.ExecuteNonQuery);
        }

        public override object? ExecuteScalar()
        {
            return ExecuteWithProfiling(_inner.ExecuteScalar);
        }

        public override void Prepare() => _inner.Prepare();

        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            return await ExecuteWithProfilingAsync(() => _inner.ExecuteReaderAsync(behavior, cancellationToken)).ConfigureAwait(false);
        }

        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            return await ExecuteWithProfilingAsync(() => _inner.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
        }

        public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            return await ExecuteWithProfilingAsync(() => _inner.ExecuteScalarAsync(cancellationToken)).ConfigureAwait(false);
        }

        private T ExecuteWithProfiling<T>(Func<T> action)
        {
            var sw = Stopwatch.StartNew();
            Exception? exception = null;
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                sw.Stop();
                TryLog(sw.ElapsedMilliseconds, exception);
            }
        }

        private async Task<T> ExecuteWithProfilingAsync<T>(Func<Task<T>> action)
        {
            var sw = Stopwatch.StartNew();
            Exception? exception = null;
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                sw.Stop();
                TryLog(sw.ElapsedMilliseconds, exception);
            }
        }

        private void TryLog(long elapsedMs, Exception? exception)
        {
            var opt = _options.Value;
            if (!opt.Enabled)
                return;

            var shouldLogError = exception != null && opt.LogOnError;
            var shouldLogSlow = exception == null && elapsedMs >= opt.ThresholdMs;
            if (!shouldLogError && !shouldLogSlow)
                return;

            var sql = CommandText ?? string.Empty;
            if (opt.MaxCommandTextLength > 0 && sql.Length > opt.MaxCommandTextLength)
                sql = sql.Substring(0, opt.MaxCommandTextLength);

            string? database = null;
            if (opt.IncludeDatabase)
            {
                try
                {
                    database = _inner.Connection?.Database;
                }
                catch
                {
                    database = null;
                }
            }

            string? traceId = null;
            string? spanId = null;
            if (opt.IncludeTrace)
            {
                var act = Activity.Current;
                if (act != null)
                {
                    traceId = act.TraceId.ToString();
                    spanId = act.SpanId.ToString();
                }
            }

            string? paramText = null;
            if (opt.IncludeParameters)
            {
                paramText = BuildParametersText(opt);
            }

            if (exception != null)
            {
                _logger.LogError(exception,
                    "DbSqlError ElapsedMs={ElapsedMs} Db={Db} TraceId={TraceId} SpanId={SpanId} Sql={Sql} Params={Params}",
                    elapsedMs, database, traceId, spanId, sql, paramText);
                return;
            }

            _logger.LogWarning(
                "SlowSql ElapsedMs={ElapsedMs} Db={Db} TraceId={TraceId} SpanId={SpanId} Sql={Sql} Params={Params}",
                elapsedMs, database, traceId, spanId, sql, paramText);
        }

        private string BuildParametersText(SlowSqlOptions opt)
        {
            var ps = DbParameterCollection.Cast<DbParameter>().ToList();
            if (ps.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            for (var i = 0; i < ps.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                var p = ps[i];
                var name = p.ParameterName ?? string.Empty;
                sb.Append(name);
                sb.Append('=');
                sb.Append(FormatParameterValue(name, p.Value, opt));
            }

            return sb.ToString();
        }

        private string FormatParameterValue(string name, object? value, SlowSqlOptions opt)
        {
            if (IsSensitiveName(name, opt))
                return "***";

            if (value == null || value == DBNull.Value)
                return "NULL";

            string text;
            try
            {
                text = Convert.ToString(value) ?? string.Empty;
            }
            catch
            {
                text = value.GetType().FullName ?? "<value>";
            }

            if (opt.MaxParameterValueLength > 0 && text.Length > opt.MaxParameterValueLength)
                text = text.Substring(0, opt.MaxParameterValueLength);

            return text;
        }

        private static bool IsSensitiveName(string name, SlowSqlOptions opt)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var candidates = opt.SensitiveParameterNames;
            if (candidates == null || candidates.Length == 0)
                return false;

            for (var i = 0; i < candidates.Length; i++)
            {
                var s = candidates[i];
                if (string.IsNullOrWhiteSpace(s))
                    continue;

                if (name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
