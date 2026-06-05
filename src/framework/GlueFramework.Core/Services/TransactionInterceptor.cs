using Castle.Core.Internal;
using Castle.DynamicProxy;
using GlueFramework.Core.Abstractions;
using System.Linq;
using System.Reflection;

namespace GlueFramework.Core.Services
{
    public sealed class TransactionInterceptor : IInterceptor
    {
        private static bool CheckMethodReturnTypeIsTaskType(MethodInfo method)
        {
            var methodReturnType = method.ReturnType;
            if (methodReturnType.IsGenericType)
            {
                if (methodReturnType.GetGenericTypeDefinition() == typeof(Task<>) ||
                    methodReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                    return true;
            }
            else
            {
                if (methodReturnType == typeof(Task) ||
                    methodReturnType == typeof(ValueTask))
                    return true;
            }

            return false;
        }

        private static bool IsTaskLikeReturn(MethodInfo method) => CheckMethodReturnTypeIsTaskType(method);

        private static bool HasTransactionalAttribute(IInvocation invocation)
        {
            var interfaceAttrs = invocation.Method.GetAttributes<TransactionalAttribute>();
            if (interfaceAttrs != null && interfaceAttrs.Any())
                return true;

            var targetMethod = invocation.MethodInvocationTarget;
            if (targetMethod != null)
            {
                var targetAttrs = targetMethod.GetAttributes<TransactionalAttribute>();
                if (targetAttrs != null && targetAttrs.Any())
                    return true;
            }

            return false;
        }

        public void Intercept(IInvocation invocation)
        {
            if (!HasTransactionalAttribute(invocation))
            {
                invocation.Proceed();
                return;
            }

            if (invocation.InvocationTarget is not ServiceBase service)
            {
                invocation.Proceed();
                return;
            }

            // Nested Transactional flow (including cross-service): reuse the ambient transaction.
            if (AmbientTransactionContext.HasActive)
            {
                invocation.Proceed();
                return;
            }

            var tx = service.BeginTransaction();

            try
            {
                invocation.Proceed();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                try { tx.Dispose(); } catch { }
                AmbientTransactionContext.Clear();
                throw;
            }

            if (IsTaskLikeReturn(invocation.MethodInvocationTarget ?? invocation.Method))
            {
                invocation.ReturnValue = InterceptAsync((dynamic)invocation.ReturnValue, tx);
                return;
            }

            try
            {
                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
            finally
            {
                tx.Dispose();
                AmbientTransactionContext.Clear();
            }
        }

        private async Task InterceptAsync(Task task, System.Data.IDbTransaction tx)
        {
            try
            {
                await task.ConfigureAwait(false);
                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
            finally
            {
                tx.Dispose();
                AmbientTransactionContext.Clear();
            }
        }

        private async Task<TResult> InterceptAsync<TResult>(Task<TResult> task, System.Data.IDbTransaction tx)
        {
            try
            {
                var result = await task.ConfigureAwait(false);
                tx.Commit();
                return result;
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
            finally
            {
                tx.Dispose();
                AmbientTransactionContext.Clear();
            }
        }

        private async ValueTask InterceptAsync(ValueTask task, System.Data.IDbTransaction tx)
        {
            try
            {
                await task.ConfigureAwait(false);
                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
            finally
            {
                tx.Dispose();
                AmbientTransactionContext.Clear();
            }
        }

        private async ValueTask<TResult> InterceptAsync<TResult>(ValueTask<TResult> task, System.Data.IDbTransaction tx)
        {
            try
            {
                var result = await task.ConfigureAwait(false);
                tx.Commit();
                return result;
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
            finally
            {
                tx.Dispose();
                AmbientTransactionContext.Clear();
            }
        }
    }
}
