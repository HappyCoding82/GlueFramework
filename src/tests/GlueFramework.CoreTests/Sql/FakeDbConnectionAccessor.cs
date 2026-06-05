using GlueFramework.Core.Abstractions;
using System;
using System.Data.Common;

namespace GlueFramework.CoreTests.Sql
{
    internal sealed class FakeDbConnectionAccessor : IDbConnectionAccessor
    {
        private readonly Func<DbConnection> _factory;

        public FakeDbConnectionAccessor(Func<DbConnection> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public DbConnection CreateConnection()
        {
            return _factory();
        }
    }
}
