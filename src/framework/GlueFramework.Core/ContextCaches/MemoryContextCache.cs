using Microsoft.Extensions.Caching.Memory;

namespace GlueFramework.Core.ContextCaches
{
    public class MemoryContextCache : IContextCache
    {
        private IMemoryCache _memoryCache;
        private static List<string> _keys = new List<string>();
        public MemoryContextCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public List<string> Keys
        {
            get
            {
                lock (_keys)
                {
                    return _keys;
                }
            }
        }

        public object Get(string key)
        {
            return _memoryCache.Get(key);
        }

        public M Get<M>(string key)
        {
            return _memoryCache.Get<M>(key);
        }

        public void Set<M>(string key, M value,int? slidingExpirationMinutes = 1)
        {
            lock (_keys) 
            {
                if(!_keys.Contains(key))
                    _keys.Add(key);
            }
            _memoryCache.Set<M>(key, value,new MemoryCacheEntryOptions() 
            { 
                 SlidingExpiration = TimeSpan.FromMinutes(slidingExpirationMinutes??1)
            });
        }

        public void Remove(string key)
        {
            lock (_keys) 
            {
                _keys.Remove(key);
            }
            _memoryCache.Remove(key);
        }
    }
}
