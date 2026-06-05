using System.Text.Json;

namespace GlueFramework.OutboxModule.Services
{
    internal static class OutboxJson
    {
        internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    }
}
