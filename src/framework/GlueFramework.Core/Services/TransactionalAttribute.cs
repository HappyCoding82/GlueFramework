using System;

namespace GlueFramework.Core.Services
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class TransactionalAttribute : Attribute
    {
    }
}
