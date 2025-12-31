using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace GlueFramework.Core.ORM
{
    public  class PreCompileGetterHelper<T>
    {
        private static readonly Lazy<ConcurrentDictionary<string, Func<T, object>>> _getters =
            new Lazy<ConcurrentDictionary<string, Func<T, object>>>(
                () =>
                {
                    var dict = new ConcurrentDictionary<string, Func<T, object>>();
                    var properties = typeof(T).GetProperties();
                    foreach (var property in properties)
                    {
                        dict.TryAdd(property.Name, CompileGetter(property.Name));
                    }
                    return dict;
                },
                isThreadSafe: true);

        public static ConcurrentDictionary<string, Func<T, object>> Getters => _getters.Value;

        private static Func<T, object> CompileGetter(string propName)
        {
            // create Expression parameter
            var param = Expression.Parameter(typeof(T), "obj");

            // create Expression tree
            var propertyExpr = Expression.Property(param, propName);

            // creat Lambda expression
            var lambda = Expression.Lambda<Func<T, object>>(Expression.Convert(propertyExpr, typeof(object)), param);

            // compile delegate
            var getter = lambda.Compile();
            return getter;
        }
    }
}
