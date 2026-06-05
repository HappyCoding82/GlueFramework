using System;

namespace GlueFramework.WebCore.Exceptions
{
    public class BusinessAcceptedException : Exception
    {
        public BusinessAcceptedException()
        {
        }

        public BusinessAcceptedException(string errorMessage) : base(errorMessage)
        {
        }
    }
}
