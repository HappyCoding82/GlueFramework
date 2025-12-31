namespace Demo.DDD.OrchardCore.Application
{
    public interface IDemoOutboxTestAppService
    {
        Task<string> TriggerAsync(string? message, CancellationToken cancellationToken = default);
    }
}
