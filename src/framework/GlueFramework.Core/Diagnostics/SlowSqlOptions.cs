using System;

namespace GlueFramework.Core.Diagnostics
{
    public sealed class SlowSqlOptions
    {
        public bool Enabled { get; set; } = false;

        public int ThresholdMs { get; set; } = 200;

        public bool LogOnError { get; set; } = true;

        public bool IncludeTrace { get; set; } = true;

        public bool IncludeDatabase { get; set; } = true;

        public bool IncludeParameters { get; set; } = false;

        public string[] SensitiveParameterNames { get; set; } = Array.Empty<string>();

        public int MaxParameterValueLength { get; set; } = 256;

        public int MaxCommandTextLength { get; set; } = 4096;
    }
}
