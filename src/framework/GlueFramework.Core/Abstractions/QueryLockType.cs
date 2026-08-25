namespace GlueFramework.Core.Abstractions
{
    /// <summary>
    /// Row-level lock hints for generated SELECT queries.
    /// </summary>
    public enum QueryLockType
    {
        /// <summary>
        /// No lock hint (default).</summary>
        None,

        /// <summary>
        /// Exclusive row lock.
        /// Maps to SQL Server UPDLOCK/ROWLOCK/HOLDLOCK, PostgreSQL/MySQL FOR UPDATE.
        /// </summary>
        ForUpdate,

        /// <summary>
        /// Shared row lock.
        /// Maps to SQL Server HOLDLOCK/ROWLOCK, PostgreSQL FOR SHARE, MySQL LOCK IN SHARE MODE.
        /// </summary>
        ForShare,

        /// <summary>
        /// PostgreSQL-specific: FOR NO KEY UPDATE.
        /// </summary>
        ForNoKeyUpdate,

        /// <summary>
        /// PostgreSQL-specific: FOR KEY SHARE.
        /// </summary>
        ForKeyShare
    }
}
