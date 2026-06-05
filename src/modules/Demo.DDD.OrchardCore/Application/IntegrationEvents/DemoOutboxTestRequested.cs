using GlueFramework.Core.Abstractions.Outbox;

namespace Demo.DDD.OrchardCore.Application.IntegrationEvents
{
    public sealed record DemoOutboxTestRequested(string RequestId, string Message) : IIntegrationEvent;
}
