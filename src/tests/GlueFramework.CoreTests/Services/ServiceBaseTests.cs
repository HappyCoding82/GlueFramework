using Castle.DynamicProxy;
using GlueFramework.Core.Abstractions;
using GlueFramework.Core.Services;
using GlueFramework.Core.UOW;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace GlueFramework.CoreTests.Services
{
    [TestClass]
    public sealed class ServiceBaseTests
    {
        [TestMethod]
        public void OpenDbSessionScope_NoTransactional_CreatesNewConnectionEachTime()
        {
            var accessor = new FakeDbConnectionAccessor();
            var svc = new TestServiceA(accessor);

            using var s1 = svc.OpenSessionForTest();
            using var s2 = svc.OpenSessionForTest();

            Assert.AreNotSame(s1.Connection, s2.Connection);
            Assert.IsNull(s1.Transaction);
            Assert.IsNull(s2.Transaction);
        }

        [TestMethod]
        public async Task Transactional_CrossService_SharesSameConnectionAndTransaction_AndRollsBack()
        {
            var accessor = new FakeDbConnectionAccessor();
            var proxyGen = new ProxyGenerator();
            var interceptor = new TransactionInterceptor();

            var a = proxyGen.CreateInterfaceProxyWithTarget<ITestServiceA>(
                new TestServiceA(accessor), interceptor);
            var b = proxyGen.CreateInterfaceProxyWithTarget<ITestServiceB>(
                new TestServiceB(accessor), interceptor);

            try
            {
                await a.DoWorkAndFailAsync(b);
                Assert.Fail("Expected exception was not thrown.");
            }
            catch (InvalidOperationException)
            {
                // expected
            }

            // Ensure async interception finally blocks have a chance to run.
            await Task.Yield();

            // After rollback, ambient context must be cleared
            Assert.IsFalse(AmbientTransactionContext.HasActive);

            // Ensure we used a single connection and single transaction for the entire flow
            Assert.AreEqual(1, accessor.CreatedConnections.Count);
            Assert.AreEqual(1, accessor.BegunTransactions.Count);

            Assert.AreSame(accessor.CreatedConnections[0], accessor.BegunTransactions[0].Connection);
            Assert.IsTrue(accessor.BegunTransactions[0].WasRolledBack);
            Assert.IsFalse(accessor.BegunTransactions[0].WasCommitted);
        }

        [TestMethod]
        public async Task Transactional_Rollback_DoesNotRunAfterCommitActions()
        {
            var accessor = new FakeDbConnectionAccessor();
            var proxyGen = new ProxyGenerator();
            var interceptor = new TransactionInterceptor();
            var a = proxyGen.CreateInterfaceProxyWithTarget<ITestServiceA>(
                new TestServiceA(accessor), interceptor);

            var afterCommitRan = false;
            try
            {
                await a.EnqueueAfterCommitAndFailAsync(() => afterCommitRan = true);
                Assert.Fail("Expected exception was not thrown.");
            }
            catch (InvalidOperationException)
            {
                // expected
            }

            // Ensure async interception finally blocks have a chance to run.
            await Task.Yield();

            Assert.IsFalse(afterCommitRan);
            Assert.IsFalse(AmbientTransactionContext.HasActive);
        }

        public interface ITestServiceA
        {
            Task DoWorkAndFailAsync(ITestServiceB b);
            Task EnqueueAfterCommitAndFailAsync(Action action);
        }

        public interface ITestServiceB
        {
            Task TouchDbAsync();
        }

        private sealed class TestServiceA : ServiceBase, ITestServiceA
        {
            public TestServiceA(IDbConnectionAccessor accessor) : base(accessor) { }

            public DbSessionScope OpenSessionForTest() => OpenDbSessionScope();

            [Transactional]
            public async Task DoWorkAndFailAsync(ITestServiceB b)
            {
                // A uses DB
                using (var s = OpenDbSessionScope())
                {
                    Assert.IsNotNull(s.Transaction);
                }

                // B uses DB too, should share same tx/conn
                await b.TouchDbAsync();

                throw new InvalidOperationException("fail");
            }

            [Transactional]
            public Task EnqueueAfterCommitAndFailAsync(Action action)
            {
                TransactionScopeContext.EnqueueAfterCommit(action);
                throw new InvalidOperationException("fail");
            }
        }

        private sealed class TestServiceB : ServiceBase, ITestServiceB
        {
            public TestServiceB(IDbConnectionAccessor accessor) : base(accessor) { }

            public Task TouchDbAsync()
            {
                using var s = OpenDbSessionScope();
                Assert.IsNotNull(s.Transaction);
                return Task.CompletedTask;
            }
        }

        private sealed class FakeDbConnectionAccessor : IDbConnectionAccessor
        {
            public List<FakeDbConnection> CreatedConnections { get; } = new();
            public List<FakeDbTransaction> BegunTransactions { get; } = new();

            public DbConnection CreateConnection()
            {
                var conn = new FakeDbConnection(this);
                CreatedConnections.Add(conn);
                return conn;
            }

            internal void RegisterTx(FakeDbTransaction tx) => BegunTransactions.Add(tx);
        }

        private sealed class FakeDbConnection : DbConnection
        {
            private readonly FakeDbConnectionAccessor _accessor;
            private ConnectionState _state = ConnectionState.Closed;

            public FakeDbConnection(FakeDbConnectionAccessor accessor)
            {
                _accessor = accessor;
            }

            public override string ConnectionString { get; set; } = "";
            public override string Database => "fake";
            public override string DataSource => "fake";
            public override string ServerVersion => "1";
            public override ConnectionState State => _state;

            public override void Open() => _state = ConnectionState.Open;
            public override void Close() => _state = ConnectionState.Closed;

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            {
                var tx = new FakeDbTransaction(this, isolationLevel);
                _accessor.RegisterTx(tx);
                return tx;
            }

            protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
            public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        }

        private sealed class FakeDbTransaction : DbTransaction
        {
            private readonly FakeDbConnection _conn;
            private readonly IsolationLevel _level;

            public FakeDbTransaction(FakeDbConnection conn, IsolationLevel level)
            {
                _conn = conn;
                _level = level;
            }

            public bool WasCommitted { get; private set; }
            public bool WasRolledBack { get; private set; }

            public override IsolationLevel IsolationLevel => _level;
            protected override DbConnection DbConnection => _conn;
            public FakeDbConnection Connection => _conn;

            public override void Commit() => WasCommitted = true;
            public override void Rollback() => WasRolledBack = true;
        }
    }
}
