namespace Foodtrucks.Api.Services
{
    public interface IPaymentService
    {
        Task<bool> ProcessPaymentAsync(decimal amount, string currency, string token);
    }

    public class MockPaymentService : IPaymentService
    {
        private readonly ILogger<MockPaymentService> _logger;

        public MockPaymentService(ILogger<MockPaymentService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> ProcessPaymentAsync(decimal amount, string currency, string token)
        {
            _logger.LogInformation($"Processing mock payment of {amount} {currency} with token {token}");
            await Task.Delay(500); // Simulate network latency
            return true; // Always succeed
        }
    }
}
