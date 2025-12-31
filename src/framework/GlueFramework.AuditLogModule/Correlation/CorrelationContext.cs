using GlueFramework.AuditLog.Abstractions;
using System;
using System.Threading;

namespace GlueFramework.AuditLogModule.Correlation
{
    public sealed class CorrelationContext : ICorrelationContext
    {
        private static readonly AsyncLocal<State?> _state = new();

        public string? CorrelationId => _state.Value?.CorrelationId;

        public string? Tenant => _state.Value?.Tenant;

        public string? User => _state.Value?.User;

        public IDisposable Begin(string correlationId, string? tenant, string? user)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException("correlationId is required", nameof(correlationId));

            var prior = _state.Value;
            _state.Value = new State(correlationId, tenant, user);
            return new PopWhenDisposed(prior);
        }

        private sealed record State(string CorrelationId, string? Tenant, string? User);

        private sealed class PopWhenDisposed : IDisposable
        {
            private readonly State? _prior;
            private bool _disposed;

            public PopWhenDisposed(State? prior) => _prior = prior;

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                _state.Value = _prior;
            }
        }
    }
}
