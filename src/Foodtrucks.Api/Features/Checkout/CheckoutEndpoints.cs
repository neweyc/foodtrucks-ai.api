using FluentValidation;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Features.Orders;
using Foodtrucks.Api.Routing;
using Foodtrucks.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Features.Checkout
{
    public class CheckoutRequest
    {
        public int TruckId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public List<PlaceOrderItemDto> Items { get; set; } = new();
    }

    public class CheckoutValidator : AbstractValidator<CheckoutRequest>
    {
        public CheckoutValidator()
        {
            RuleFor(x => x.TruckId).GreaterThan(0);
            RuleFor(x => x.CustomerName).NotEmpty();
            RuleFor(x => x.CustomerPhone).NotEmpty();
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).ChildRules(items =>
            {
                items.RuleFor(i => i.Quantity).GreaterThan(0);
            });
        }
    }

    public class CheckoutEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/checkout", async (
                [FromBody] CheckoutRequest request,
                AppDbContext db,
                IStripeService stripeService,
                IValidator<CheckoutRequest> validator,
                ILogger<CheckoutEndpoints> logger,
                IConfiguration config,
                CancellationToken ct) =>
            {
                try
                {
                    var validationResult = await validator.ValidateAsync(request, ct);
                    if (!validationResult.IsValid)
                    {
                        return Results.ValidationProblem(validationResult.ToDictionary());
                    }

                    // 1. Fetch Truck
                    var truck = await db.Trucks.FindAsync(new object[] { request.TruckId }, ct);
                    if (truck == null) return Results.NotFound("Truck not found");

                    // 2. Validate Items and Calculate Total
                    var itemIds = request.Items.Select(i => i.MenuItemId).ToList();
                    var menuItems = await db.MenuItems
                        .Include(m => m.MenuCategory)
                        .Include(m => m.Sizes)
                        .Include(m => m.Options)
                        .Where(m => itemIds.Contains(m.Id))
                        .ToDictionaryAsync(m => m.Id, m => m, ct);

                    decimal totalAmount = 0;
                    var orderItems = new List<OrderItem>();

                    foreach (var itemDto in request.Items)
                    {
                        if (!menuItems.TryGetValue(itemDto.MenuItemId, out var menuItem))
                        {
                            return Results.NotFound($"MenuItem {itemDto.MenuItemId} not found");
                        }

                        if (menuItem.MenuCategory == null || menuItem.MenuCategory.TruckId != request.TruckId)
                        {
                            return Results.BadRequest($"MenuItem {itemDto.MenuItemId} (\"{menuItem.Name}\") does not belong to Truck {request.TruckId}");
                        }

                        decimal itemPrice = menuItem.Price;
                        string? selectedSizeName = null;
                        
                        // Size Logic
                        if (menuItem.Sizes.Any())
                        {
                            if (!itemDto.SizeId.HasValue)
                            {
                                 return Results.BadRequest($"Size is required for item {menuItem.Name}");
                            }
                            var size = menuItem.Sizes.FirstOrDefault(s => s.Id == itemDto.SizeId.Value);
                            if (size == null)
                            {
                                return Results.BadRequest($"Invalid size for item {menuItem.Name}");
                            }
                            itemPrice = size.Price;
                            selectedSizeName = size.Name;
                        }

                        // Options Logic
                        var selectedOptionNames = new List<string>();
                        if (itemDto.OptionIds != null && itemDto.OptionIds.Any())
                        {
                            foreach (var optId in itemDto.OptionIds)
                            {
                                var option = menuItem.Options.FirstOrDefault(o => o.Id == optId);
                                if (option == null)
                                {
                                    return Results.BadRequest($"Invalid option {optId} for item {menuItem.Name}");
                                }
                                itemPrice += option.Price;
                                selectedOptionNames.Add(option.Name);
                            }
                        }

                        totalAmount += itemPrice * itemDto.Quantity;
                        orderItems.Add(new OrderItem
                        {
                            MenuItemId = menuItem.Id,
                            ItemName = menuItem.Name,
                            Price = itemPrice,
                            Quantity = itemDto.Quantity,
                            SelectedSize = selectedSizeName,
                            SelectedOptions = selectedOptionNames.Any() ? string.Join(", ", selectedOptionNames) : null
                        });
                    }

                    // 3. Create Pending Order
                    var order = new Order
                    {
                        TruckId = request.TruckId,
                        CustomerName = request.CustomerName,
                        CustomerPhone = request.CustomerPhone,
                        TotalAmount = totalAmount,
                        Status = OrderStatus.Pending, // Pending Payment
                        Items = orderItems
                    };

                    db.Orders.Add(order);
                    await db.SaveChangesAsync(ct);

                    // 4. Create Stripe Session
                    string domain = "http://localhost:3000"; 
                    if (config["UiUrl"] != null) domain = config["UiUrl"]!;
                    
                    // Fetch Vendor to get StripeAccountId
                    // Optimisation: We already fetched Truck, checking if we included Vendor
                    // Truck entity might not have Vendor included by default if not requested.
                    // Need to include it or fetch it.
                    var vendor = await db.Vendors.FindAsync(new object[] { truck.VendorId }, ct);
                    
                    var session = await stripeService.CreateCheckoutSessionAsync(order, orderItems, domain, vendor?.StripeAccountId);

                    return Results.Ok(new { url = session.Url });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error creating checkout session");
                    return Results.Problem(ex.Message);
                }
            })
            .WithTags("Checkout")
            .WithOpenApi();

            app.MapPost("/api/checkout/verify", async (
                [FromBody] VerifyCheckoutRequest request,
                IStripeService stripeService,
                AppDbContext db,
                ISmsService smsService,
                ILogger<CheckoutEndpoints> logger,
                CancellationToken ct) =>
            {
                try 
                {
                    logger.LogInformation($"Verifying checkout session {request.SessionId}");
                    var session = await stripeService.GetSessionAsync(request.SessionId);
                    
                    if (session == null || session.PaymentStatus != "paid")
                    {
                        logger.LogWarning($"Session {request.SessionId} payment status: {session?.PaymentStatus}");
                        return Results.BadRequest("Payment not completed or session invalid");
                    }

                    if (!session.Metadata.TryGetValue("OrderId", out var orderIdStr) || !int.TryParse(orderIdStr, out var orderId))
                    {
                         return Results.BadRequest("Invalid session metadata");
                    }

                    var order = await db.Orders.FindAsync(new object[] { orderId }, ct);
                    if (order == null)
                    {
                         return Results.NotFound("Order not found");
                    }

                    if (order.Status == OrderStatus.Pending)
                    {
                        order.Status = OrderStatus.Paid;
                        await db.SaveChangesAsync(ct);
                        
                        await smsService.SendSmsAsync(order.CustomerPhone, $"Order #{order.Id} confirmed! Amt: {order.TotalAmount:C}. Track: /orders/{order.TrackingCode}");
                    }

                    return Results.Ok(new { orderId = order.Id, trackingCode = order.TrackingCode });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error verifying checkout");
                    return Results.Problem("Failed to verify checkout");
                }
            })
            .WithTags("Checkout")
            .WithOpenApi();
        }
    }

    public class VerifyCheckoutRequest
    {
        public string SessionId { get; set; } = string.Empty;
    }
}
