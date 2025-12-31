using Castle.DynamicProxy;
using GlueFramework.AuditLog.Abstractions;
using GlueFramework.AuditLogModule.Correlation;
using GlueFramework.AuditLogModule.Options;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GlueFramework.AuditLogModule.Interceptors
{
    public sealed class AuditStepInterceptor : IInterceptor
    {
        private readonly IOptions<AuditLogOptions> _options;
        private readonly IAuditWriter _writer;
        private readonly CorrelationContext _correlation;

        public AuditStepInterceptor(IOptions<AuditLogOptions> options, IAuditWriter writer, CorrelationContext correlation)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _correlation = correlation ?? throw new ArgumentNullException(nameof(correlation));
        }

        public void Intercept(IInvocation invocation)
        {
            var attr = GetAuditStepAttribute(invocation);
            if (attr == null)
            {
                invocation.Proceed();
                return;
            }

            if (!_options.Value.Enabled)
            {
                invocation.Proceed();
                return;
            }

            var correlationId = _correlation.CorrelationId;
            if (string.IsNullOrWhiteSpace(correlationId) && attr.RequireCorrelation)
            {
                invocation.Proceed();
                return;
            }

            if (string.IsNullOrWhiteSpace(correlationId))
                correlationId = Guid.NewGuid().ToString("N");

            using var scope = _correlation.Begin(correlationId!, _correlation.Tenant, _correlation.User);

            var sw = Stopwatch.StartNew();

            var evt = new AuditEvent
            {
                Action = attr.Action,
                CorrelationId = correlationId,
                Tenant = _correlation.Tenant,
                User = _correlation.User,
                TraceId = Activity.Current?.TraceId.ToString(),
                SpanId = Activity.Current?.SpanId.ToString(),
                OccurredUtc = DateTimeOffset.UtcNow,
                ArgsJson = attr.IncludeArgs ? SafeSerialize(invocation.Arguments) : null,
            };

            try
            {
                invocation.Proceed();
            }
            catch (Exception ex)
            {
                sw.Stop();
                evt.ElapsedMs = sw.ElapsedMilliseconds;
                evt.Success = false;
                if (attr.IncludeException)
                    evt.Exception = ex.ToString();

                _ = TryWriteAsync(evt);
                throw;
            }

            if (IsTaskLikeReturn(invocation.MethodInvocationTarget ?? invocation.Method))
            {
                invocation.ReturnValue = InterceptAsync((dynamic)invocation.ReturnValue, evt, attr, sw);
                return;
            }

            sw.Stop();
            evt.ElapsedMs = sw.ElapsedMilliseconds;
            evt.Success = true;
            if (attr.IncludeResult)
            {
                evt.ResultJson = SafeSerialize(invocation.ReturnValue);
            }

            _ = TryWriteAsync(evt);
        }

        private async Task InterceptAsync(Task task, AuditEvent evt, AuditStepAttribute attr, Stopwatch sw)
        {
            try
            {
                await task.ConfigureAwait(false);
                sw.Stop();
                evt.ElapsedMs = sw.ElapsedMilliseconds;
                evt.Success = true;
                await TryWriteAsync(evt).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                sw.Stop();
                evt.ElapsedMs = sw.ElapsedMilliseconds;
                evt.Success = false;
                if (attr.IncludeException)
                    evt.Exception = ex.ToString();
                await TryWriteAsync(evt).ConfigureAwait(false);
                throw;
            }
        }

        private async Task<T> InterceptAsync<T>(Task<T> task, AuditEvent evt, AuditStepAttribute attr, Stopwatch sw)
        {
            try
            {
                var result = await task.ConfigureAwait(false);
                sw.Stop();
                evt.ElapsedMs = sw.ElapsedMilliseconds;
                evt.Success = true;
                if (attr.IncludeResult)
                    evt.ResultJson = SafeSerialize(result);
                await TryWriteAsync(evt).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                evt.ElapsedMs = sw.ElapsedMilliseconds;
                evt.Success = false;
                if (attr.IncludeException)
                    evt.Exception = ex.ToString();
                await TryWriteAsync(evt).ConfigureAwait(false);
                throw;
            }
        }

        private static bool IsTaskLikeReturn(System.Reflection.MethodInfo method)
        {
            var t = method.ReturnType;
            return typeof(Task).IsAssignableFrom(t);
        }

        private static AuditStepAttribute? GetAuditStepAttribute(IInvocation invocation)
        {
            var m = invocation.MethodInvocationTarget ?? invocation.Method;
            return (AuditStepAttribute?)Attribute.GetCustomAttribute(m, typeof(AuditStepAttribute), inherit: true)
                ?? (AuditStepAttribute?)Attribute.GetCustomAttribute(invocation.Method, typeof(AuditStepAttribute), inherit: true);
        }

        private Task TryWriteAsync(AuditEvent evt)
        {
            try
            {
                return _writer.WriteAsync(evt, CancellationToken.None);
            }
            catch
            {
                return Task.CompletedTask;
            }
        }

        private static string? SafeSerialize(object? obj)
        {
            try
            {
                return JsonSerializer.Serialize(obj, new JsonSerializerOptions
                {
                    WriteIndented = false
                });
            }
            catch
            {
                return null;
            }
        }
    }
}
