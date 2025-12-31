namespace Demo.DDD.OrchardCore.Infrastructure
{
    public interface IStripePayments
    {
        Task<string> CreatePaymentIntentAsync(string orderId, decimal amount);

        Task<string> CreatePayoutAsync(string orderId, string sellerId, decimal amount);
    }

    public sealed class StripePaymentsStub : IStripePayments
    {
        public Task<string> CreatePaymentIntentAsync(string orderId, decimal amount)
        {
            return Task.FromResult("pi_" + Guid.NewGuid().ToString("N"));
        }

        public Task<string> CreatePayoutAsync(string orderId, string sellerId, decimal amount)
        {
            return Task.FromResult("po_" + Guid.NewGuid().ToString("N"));
        }
    }
}
