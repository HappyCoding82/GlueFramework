using System;

namespace GlueFramework.WebCore.Exceptions
{
    public class DataScopeNotPermittedException: Exception
    {
        public DataScopeNotPermittedException()
        {
        }

        public DataScopeNotPermittedException(string errorMessage) : base(errorMessage)
        {
        }
    }
}
