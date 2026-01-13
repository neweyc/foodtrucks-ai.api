using Stripe;
using Stripe.Checkout;
using Foodtrucks.Api.Features.Orders;

namespace Foodtrucks.Api.Services
{
    public interface IStripeService
    {
        Task<Session> CreateCheckoutSessionAsync(Order order, List<OrderItem> items, string domain, string? vendorStripeAccountId = null);
        Task<Session> GetSessionAsync(string sessionId);
        Task<string> CreateVendorAccountAsync(string businessName, int vendorId);
        Task<string> CreateAccountLinkAsync(string accountId, int vendorId);
        Task<bool> IsAccountCompleteAsync(string accountId);
    }

    public class StripeService : IStripeService
    {
        private readonly IConfiguration _configuration;

        public StripeService(IConfiguration configuration)
        {
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<Session> CreateCheckoutSessionAsync(Order order, List<OrderItem> items, string domain, string? vendorStripeAccountId = null)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = $"{domain}/checkout/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/checkout/cancel?order_id={order.Id}",
                Metadata = new Dictionary<string, string>
                {
                    { "OrderId", order.Id.ToString() }
                }
            };

            // If Vendor is connected, apply split payment
            if (!string.IsNullOrEmpty(vendorStripeAccountId))
            {
                // Calculate Fee (e.g., 10%)
                var totalCents = (long)(order.TotalAmount * 100);
                var feeCents = (long)(totalCents * 0.10m); 

                options.PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    ApplicationFeeAmount = feeCents,
                    TransferData = new SessionPaymentIntentDataTransferDataOptions
                    {
                        Destination = vendorStripeAccountId,
                    },
                };
            }

            foreach (var item in items)
            {
                var description = item.ItemName;
                if (!string.IsNullOrEmpty(item.SelectedSize))
                {
                    description += $" ({item.SelectedSize})";
                }
                if (!string.IsNullOrEmpty(item.SelectedOptions))
                {
                    description += $" + {item.SelectedOptions}";
                }

                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmountDecimal = item.Price * 100, // Stripe expects amounts in cents
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.ItemName,
                            Description = description.Length > 500 ? description.Substring(0, 500) : description
                        },
                    },
                    Quantity = item.Quantity,
                });
            }

            var service = new SessionService();
            return await service.CreateAsync(options);
        }

        public async Task<Session> GetSessionAsync(string sessionId)
        {
            var service = new SessionService();
            return await service.GetAsync(sessionId);
        }

        public async Task<string> CreateVendorAccountAsync(string businessName, int vendorId)
        {
            var options = new AccountCreateOptions
            {
                Type = "express", // Express accounts are best for platforms controlling UI
                Country = "US",
                Email = $"vendor_{vendorId}@example.com", // Stripe requires email; using placeholder or fetch real one if available
                Capabilities = new AccountCapabilitiesOptions
                {
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                },
                BusinessType = "company",
                Company = new AccountCompanyOptions
                {
                    Name = businessName,
                },
                Metadata = new Dictionary<string, string>
                {
                    { "VendorId", vendorId.ToString() }
                }
            };
            var service = new AccountService();
            var account = await service.CreateAsync(options);
            return account.Id;
        }

        public async Task<string> CreateAccountLinkAsync(string accountId, int vendorId)
        {
            var domain = _configuration["AppUrl"] ?? "http://localhost:3000"; 
            if (!domain.StartsWith("http")) domain = "http://localhost:3000";
            if (_configuration["UiUrl"] != null) domain = _configuration["UiUrl"]!;
            
            // Build the API Return URL
            var apiBase = _configuration["ApiBaseUrl"] ?? "http://localhost:5150";
            var returnUrl = $"{apiBase}/api/vendors/{vendorId}/onboarding/return";

            var options = new AccountLinkCreateOptions
            {
                Account = accountId,
                RefreshUrl = $"{domain}/vendor/profile", 
                ReturnUrl = returnUrl,
                Type = "account_onboarding",
            };

            var service = new AccountLinkService();
            var link = await service.CreateAsync(options);
            return link.Url;
        }

        public async Task<bool> IsAccountCompleteAsync(string accountId)
        {
            var service = new AccountService();
            var account = await service.GetAsync(accountId);
            return account.DetailsSubmitted;
        }
    }
}
