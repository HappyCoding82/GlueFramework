using System;
using System.Collections.Generic;
using System.Threading;

namespace GlueFramework.Core.Abstractions
{
    public static class TransactionScopeContext
    {
        private static readonly AsyncLocal<Scope?> _current = new();

        public static bool HasActiveScope => _current.Value != null;

        public static void Begin()
        {
            if (_current.Value != null)
                throw new InvalidOperationException("Transaction scope already exists.");

            _current.Value = new Scope();
        }

        public static void EnqueueAfterCommit(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _current.Value?.AfterCommit.Add(action);
        }

        public static void Commit()
        {
            var scope = _current.Value;
            _current.Value = null;

            if (scope == null)
                return;

            foreach (var action in scope.AfterCommit)
            {
                action();
            }
        }

        public static void Rollback()
        {
            _current.Value = null;
        }

        private sealed class Scope
        {
            public List<Action> AfterCommit { get; } = new();
        }
    }
}
