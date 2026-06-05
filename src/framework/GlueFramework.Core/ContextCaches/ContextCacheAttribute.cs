using System;

namespace GlueFramework.Core.ContextCaches
{
    public class ContextCacheAttribute:Attribute
    {
        private string _key;
        private bool _isRemoval;

        public string Key 
        { 
            get 
            { 
                return _key; 
            } 
        }

        public bool IsRemoval
        {
            get
            {
                return _isRemoval;
            }
        }

        public ContextCacheAttribute(string key,bool isRemoval = false)
        {
            _key = key;
            _isRemoval = isRemoval;
        }
    }
}
