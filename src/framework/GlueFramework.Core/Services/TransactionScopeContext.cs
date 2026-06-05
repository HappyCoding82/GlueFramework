using System;
using GlueFramework.Core.Abstractions;

namespace GlueFramework.Core.Abstractions
{
    public static class TransactionScopeContext
    {
        public static bool HasActiveScope => AmbientTransactionContext.HasActive;

        public static void Begin()
        {
            // Keep backward compatibility: Begin may be called by older code paths.
            // We no longer throw on nested begin; nested Transactional flows are supported.
            AmbientTransactionContext.BeginAfterCommitScopeIfNeeded();
        }

        public static void EnqueueAfterCommit(Action action)
        {
            AmbientTransactionContext.EnqueueAfterCommit(action);
        }

        public static void Commit()
        {
            AmbientTransactionContext.CommitAfterCommitActions();
            AmbientTransactionContext.Clear();
        }

        public static void Rollback()
        {
            AmbientTransactionContext.RollbackAfterCommitActions();
            AmbientTransactionContext.Clear();
        }
    }
}
