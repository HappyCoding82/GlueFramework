using System;

namespace GlueFramework.OrchardCore.Observability.Services
{
    public sealed class ObservabilityRuntimeState
    {
        public bool Started { get; set; }

        public string? AppliedOtlpEndpoint { get; set; }

        public double AppliedTraceSampleRate { get; set; }

        public DateTimeOffset? StartedUtc { get; set; }

        public string? LastError { get; set; }

        public DateTimeOffset? LastErrorUtc { get; set; }
    }
}
