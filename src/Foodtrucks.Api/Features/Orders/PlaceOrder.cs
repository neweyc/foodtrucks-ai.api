using FluentValidation;
using Foodtrucks.Api.Commands;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Routing;
using Foodtrucks.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Features.Orders
{
    public class PlaceOrderRequest
    {
        public int TruckId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string PaymentToken { get; set; } = string.Empty;
        public List<PlaceOrderItemDto> Items { get; set; } = new();
    }

    public class PlaceOrderItemDto
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public int? SizeId { get; set; }
        public List<int> OptionIds { get; set; } = new();
    }

    public class PlaceOrderValidator : AbstractValidator<PlaceOrderRequest>
    {
        public PlaceOrderValidator()
        {
            RuleFor(x => x.TruckId).GreaterThan(0);
            RuleFor(x => x.CustomerName).NotEmpty();
            RuleFor(x => x.CustomerPhone).NotEmpty();
            RuleFor(x => x.PaymentToken).NotEmpty();
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).ChildRules(items =>
            {
                items.RuleFor(i => i.Quantity).GreaterThan(0);
            });
        }
    }


    public record OrderResultDto(int Id, string TrackingCode);

    public class PlaceOrderCommandHandler(AppDbContext db, IPaymentService paymentService, ISmsService smsService, IValidator<PlaceOrderRequest> validator, CancellationToken ct)
    {
        public async Task<CommandResult<OrderResultDto>> Handle(PlaceOrderRequest request)
        {
            try
            {
                var validationResult = await validator.ValidateAsync(request, ct);
                if (!validationResult.IsValid)
                {
                    return CommandResult<OrderResultDto>.Failure(validationResult.ToDictionary());
                }

                // 1. Fetch Truck
                var truck = await db.Trucks.FindAsync(new object[] { request.TruckId }, ct);
                if (truck == null) return CommandResult<OrderResultDto>.Failure("Truck not found");

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
                        return CommandResult<OrderResultDto>.NotFoundResult($"MenuItem {itemDto.MenuItemId} not found");
                    }

                    if (menuItem.MenuCategory == null || menuItem.MenuCategory.TruckId != request.TruckId)
                    {
                        return CommandResult<OrderResultDto>.Failure($"MenuItem {itemDto.MenuItemId} (\"{menuItem.Name}\") does not belong to Truck {request.TruckId}");
                    }

                    decimal itemPrice = menuItem.Price;
                    string? selectedSizeName = null;
                    
                    // Size Logic
                    if (menuItem.Sizes.Any())
                    {
                        if (!itemDto.SizeId.HasValue)
                        {
                             return CommandResult<OrderResultDto>.Failure($"Size is required for item {menuItem.Name}");
                        }
                        var size = menuItem.Sizes.FirstOrDefault(s => s.Id == itemDto.SizeId.Value);
                        if (size == null)
                        {
                            return CommandResult<OrderResultDto>.Failure($"Invalid size for item {menuItem.Name}");
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
                                return CommandResult<OrderResultDto>.Failure($"Invalid option {optId} for item {menuItem.Name}");
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

                // 3. Process Payment
                var paymentSuccess = await paymentService.ProcessPaymentAsync(totalAmount, "USD", request.PaymentToken);
                if (!paymentSuccess)
                {
                    return CommandResult<OrderResultDto>.Failure("Payment failed");
                }

                // 4. Save Order
                var order = new Order
                {
                    TruckId = request.TruckId,
                    CustomerName = request.CustomerName,
                    CustomerPhone = request.CustomerPhone,
                    TotalAmount = totalAmount,
                    Status = OrderStatus.Paid,
                    Items = orderItems
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync(ct);

                // 5. Send SMS
                await smsService.SendSmsAsync(request.CustomerPhone, $"Order #{order.Id} received! Total: {totalAmount:C}. Track here: /orders/{order.TrackingCode}");
                
                return CommandResult<OrderResultDto>.SuccessResult(new OrderResultDto(order.Id, order.TrackingCode));
            }
            catch(Exception ex)
            {
                return CommandResult<OrderResultDto>.Failure(ex.Message);
            }
        }
    }

    public class PlaceOrderEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/orders", async (
                [FromBody] PlaceOrderRequest request,
                AppDbContext db,
                IPaymentService paymentService,
                ISmsService smsService,
                IValidator<PlaceOrderRequest> validator,
                CancellationToken ct) =>
            {
               
                var handler = new PlaceOrderCommandHandler(db, paymentService, smsService, validator, ct);
                var result = await handler.Handle(request);
                
                if (result.IsSuccess)
                {
                    return Results.Ok(result.Data);
                }
                
                if(result.IsNotFound)
                {
                    return Results.NotFound(result.Message);
                }

                return Results.BadRequest(result.Message);

            })
            .WithTags("Orders")
            .WithOpenApi();
        }
    }
}
