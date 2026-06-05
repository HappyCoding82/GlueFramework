using System;

namespace GlueFramework.AuditLog.Abstractions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class AuditAttribute : Attribute
    {
        public AuditAttribute(string action)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public string Action { get; }

        public bool IncludeArgs { get; set; } = true;

        public bool IncludeResult { get; set; } = false;

        public bool IncludeException { get; set; } = true;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class AuditStepAttribute : Attribute
    {
        public AuditStepAttribute(string action)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public string Action { get; }

        public bool IncludeArgs { get; set; } = false;

        public bool IncludeResult { get; set; } = false;

        public bool IncludeException { get; set; } = true;

        // By default, a step should be correlated to an existing Audit/Correlation scope.
        // If CorrelationId is missing and this is true, the step will be skipped.
        public bool RequireCorrelation { get; set; } = true;
    }
}
