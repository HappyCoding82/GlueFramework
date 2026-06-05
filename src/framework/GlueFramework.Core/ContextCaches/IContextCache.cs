using System.Collections.Generic;

namespace GlueFramework.Core.ContextCaches
{
    public interface IContextCache
    {
        M Get<M>(string key);

        object Get(string key);

        void Set<M>(string key, M value,int? slidingExpirationMinutes = 1);

        void Remove(string key);

        List<string> Keys { get;  }
    }
}
