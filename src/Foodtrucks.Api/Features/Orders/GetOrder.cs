using Foodtrucks.Api.Commands;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Features.Orders
{
    public class GetOrderResponse
    {
        public int Id { get; set; }
        public int TruckId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<GetOrderItemDto> Items { get; set; } = new();
    }

    public class GetOrderItemDto
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class GetOrderQueryHandler(AppDbContext db)
    {
        public async Task<CommandResult<GetOrderResponse>> Handle(string trackingCode, CancellationToken ct)
        {
             var order = await db.Orders
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .Where(o => o.TrackingCode == trackingCode)
                    .Select(o => new GetOrderResponse
                    {
                        Id = o.Id,
                        TruckId = o.TruckId,
                        CustomerName = o.CustomerName,
                        CustomerPhone = o.CustomerPhone,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status.ToString(),
                        CreatedAt = o.CreatedAt,
                        Items = o.Items.Select(i => new GetOrderItemDto
                        {
                            ItemName = i.ItemName,
                            Quantity = i.Quantity,
                            Price = i.Price
                        }).ToList()
                    })
                    .FirstOrDefaultAsync(ct);

            if (order == null)
            {
                return CommandResult<GetOrderResponse>.NotFoundResult();
            }

            return CommandResult<GetOrderResponse>.SuccessResult(order);
        }
    }

    public class GetOrderEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/orders/{trackingCode}", async (
                string trackingCode,
                AppDbContext db,
                CancellationToken ct) =>
            {
                var handler = new GetOrderQueryHandler(db);
                var result = await handler.Handle(trackingCode, ct);

                if (result.IsNotFound)
                {
                    return Results.NotFound();
                }

                return Results.Ok(result.Data);
            })
            .WithTags("Orders")
            .WithOpenApi();
        }
    }
}
