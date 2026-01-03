namespace Foodtrucks.Api.Services
{
    public interface ISmsService
    {
        Task SendSmsAsync(string phoneNumber, string message);
    }

    public class MockSmsService : ISmsService
    {
        private readonly ILogger<MockSmsService> _logger;

        public MockSmsService(ILogger<MockSmsService> logger)
        {
            _logger = logger;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            _logger.LogInformation($"Sending mock SMS to {phoneNumber}: {message}");
            await Task.Delay(100);
        }
    }
}
