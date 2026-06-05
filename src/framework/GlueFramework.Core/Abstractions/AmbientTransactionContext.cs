using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;

namespace GlueFramework.Core.Abstractions
{
    /// <summary>
    /// Async-flow ambient transaction context used to share a single DbConnection/DbTransaction
    /// across multiple ServiceBase instances within a single Transactional call chain.
    /// </summary>
    public static class AmbientTransactionContext
    {
        private static readonly AsyncLocal<State?> _current = new();

        // IMPORTANT: AsyncLocal values are captured/restored by await continuations.
        // We must consider the transaction active only when an actual Transaction is attached,
        // not merely when a State object exists.
        public static bool HasActive => _current.Value?.Transaction != null;

        internal static DbConnection? CurrentConnection => _current.Value?.Connection;

        internal static IDbTransaction? CurrentTransaction => _current.Value?.Transaction;

        public static void EnqueueAfterCommit(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _current.Value?.AfterCommit.Add(action);
        }

        internal static void BeginAfterCommitScopeIfNeeded()
        {
            _current.Value ??= new State();
        }

        internal static void Begin(DbConnection connection, IDbTransaction transaction)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            var state = _current.Value;
            if (state != null)
            {
                // If a scope was created for AfterCommit actions before the DB transaction existed,
                // attach the connection/transaction now.
                if (state.Connection == null && state.Transaction == null && state.Depth == 0)
                {
                    state.Connection = connection;
                    state.Transaction = transaction;
                    state.Depth = 1;
                    return;
                }

                // Nested Transactional: do not create a new transaction.
                state.Depth++;
                return;
            }

            _current.Value = new State { Connection = connection, Transaction = transaction, Depth = 1 };
        }

        internal static void CommitAfterCommitActions()
        {
            var state = _current.Value;
            if (state == null)
                return;

            foreach (var action in state.AfterCommit)
            {
                action();
            }

            state.AfterCommit.Clear();

            // If no DB transaction was ever attached (AfterCommit-only scope), clear the ambient state.
            if (state.Connection == null && state.Transaction == null)
                Clear();
        }

        internal static void RollbackAfterCommitActions()
        {
            var state = _current.Value;
            if (state == null)
                return;

            state.AfterCommit.Clear();

            // If no DB transaction was ever attached (AfterCommit-only scope), clear the ambient state.
            if (state.Connection == null && state.Transaction == null)
                Clear();
        }

        internal static void Clear()
        {
            var state = _current.Value;
            if (state == null)
                return;

            state.Connection = null;
            state.Transaction = null;
            state.Depth = 0;
            state.AfterCommit.Clear();
        }

        private sealed class State
        {
            public DbConnection? Connection { get; set; }
            public IDbTransaction? Transaction { get; set; }
            public int Depth { get; set; }
            public List<Action> AfterCommit { get; } = new();
        }
    }
}
