using Castle.Core.Internal;
using Castle.DynamicProxy;
using GlueFramework.Core.Abstractions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GlueFramework.Core.ContextCaches
{
    public class MemoryCacheInterceptor : IInterceptor
    {
        private IContextCache _contextCache;

        public MemoryCacheInterceptor(IContextCache contextCache)
        {
            _contextCache = contextCache;
        }

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

        private static Type? GetTaskResultType(Type returnType)
        {
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                return returnType.GenericTypeArguments[0];

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                return returnType.GenericTypeArguments[0];

            return null;
        }

        private static ContextCacheAttribute? GetContextCacheAttribute(IInvocation invocation)
        {
            var interfaceAttrs = invocation.Method.GetAttributes<ContextCacheAttribute>();
            if (interfaceAttrs != null && interfaceAttrs.Any())
                return interfaceAttrs.First() as ContextCacheAttribute;

            var targetMethod = invocation.MethodInvocationTarget;
            if (targetMethod != null)
            {
                var targetAttrs = targetMethod.GetAttributes<ContextCacheAttribute>();
                if (targetAttrs != null && targetAttrs.Any())
                    return targetAttrs.First() as ContextCacheAttribute;
            }

            return null;
        }

        public void Intercept(IInvocation invocation)
        {
            
            MethodInfo method = invocation.Method;
            var attr = GetContextCacheAttribute(invocation);
            if (attr != null)
            {
                if (attr != null)
                {
                    string cacheKey = attr.Key;

                    if (invocation.InvocationTarget is ICacheKeyPrefixProvider prefixProvider)
                    {
                        var prefix = prefixProvider.CacheKeyPrefix;
                        if (!string.IsNullOrWhiteSpace(prefix))
                            cacheKey = $"{prefix}:{cacheKey}";
                    }
                    bool isRemoval = attr.IsRemoval;
                    if (isRemoval)
                    {
                        invocation.Proceed();

                        if (TransactionScopeContext.HasActiveScope)
                        {
                            TransactionScopeContext.EnqueueAfterCommit(() =>
                            {
                                _contextCache.Keys.Remove(cacheKey);
                                _contextCache.Remove(cacheKey);
                            });
                        }
                        else
                        {
                            _contextCache.Keys.Remove(cacheKey);
                            _contextCache.Remove(cacheKey);
                        }
                        return;
                    }

                    var returnType = (invocation.MethodInvocationTarget ?? invocation.Method).ReturnType;
                    var taskResultType = GetTaskResultType(returnType);

                    if (_contextCache.Keys.Contains(cacheKey))
                    {
                        var cachedObject = _contextCache.Get(cacheKey);
                        if (cachedObject != null)
                        {
                            if (taskResultType != null)
                            {
                                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                                {
                                    var fromResult = typeof(Task)
                                        .GetMethod(nameof(Task.FromResult))!
                                        .MakeGenericMethod(taskResultType);
                                    invocation.ReturnValue = fromResult.Invoke(null, new[] { cachedObject });
                                    return;
                                }

                                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                                {
                                    invocation.ReturnValue = Activator.CreateInstance(returnType, cachedObject);
                                    return;
                                }
                            }

                            invocation.ReturnValue = cachedObject;
                            return;
                        }
                    }

                    invocation.Proceed();

                    if (taskResultType != null)
                    {
                        invocation.ReturnValue = InterceptAsync((dynamic)invocation.ReturnValue, invocation, cacheKey);
                        return;
                    }

                    AfterProceedSync(invocation);
                    _contextCache.Set(cacheKey, invocation.ReturnValue);
                }
            }
            else
            {
                invocation.Proceed();
            }
        }

        protected object ProceedAsyncResult { get; set; }


        private async Task InterceptAsync(Task task, IInvocation invocation, string cacheKey)
        {
            await task.ConfigureAwait(false);
            await AfterProceedAsync(invocation, false);
            _contextCache.Set(cacheKey, invocation.ReturnValue);
        }

        private async Task<TResult> InterceptAsync<TResult>(Task<TResult> task, IInvocation invocation, string cacheKey)
        {
            ProceedAsyncResult = await task.ConfigureAwait(false);
            await AfterProceedAsync(invocation, true);
            _contextCache.Set(cacheKey, ProceedAsyncResult);
            return (TResult)ProceedAsyncResult;
        }

        private async ValueTask InterceptAsync(ValueTask task, IInvocation invocation, string cacheKey)
        {
            await task.ConfigureAwait(false);
            await AfterProceedAsync(invocation, false);
            _contextCache.Set(cacheKey, invocation.ReturnValue);
        }

        private async ValueTask<TResult> InterceptAsync<TResult>(ValueTask<TResult> task, IInvocation invocation, string cacheKey)
        {
            ProceedAsyncResult = await task.ConfigureAwait(false);
            await AfterProceedAsync(invocation, true);
            _contextCache.Set(cacheKey, ProceedAsyncResult);
            return (TResult)ProceedAsyncResult;
        }

        protected virtual void BeforeProceed(IInvocation invocation) { }

        protected virtual void AfterProceedSync(IInvocation invocation) { }

        protected virtual Task AfterProceedAsync(IInvocation invocation, bool hasAsynResult)
        {
            return Task.CompletedTask;
        }
    }
}
