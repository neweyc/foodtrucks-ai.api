using Foodtrucks.Api.Commands;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Features.Orders
{
    public class GetOrdersResponse
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<GetOrdersItemDto> Items { get; set; } = new();
    }

    public class GetOrdersItemDto
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class GetOrdersQueryHandler(AppDbContext db)
    {
        public async Task<CommandResult<List<GetOrdersResponse>>> Handle(int truckId, CancellationToken ct)
        {
            var orders = await db.Orders
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .Where(o => o.TruckId == truckId)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new GetOrdersResponse
                    {
                        Id = o.Id,
                        CustomerName = o.CustomerName,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status.ToString(),
                        CreatedAt = o.CreatedAt,
                        Items = o.Items.Select(i => new GetOrdersItemDto
                        {
                            ItemName = i.ItemName,
                            Quantity = i.Quantity,
                            Price = i.Price
                        }).ToList()
                    })
                    .ToListAsync(ct);

            return CommandResult<List<GetOrdersResponse>>.SuccessResult(orders);
        }
    }

    public class GetOrdersEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/trucks/{truckId}/orders", async (
                int truckId,
                AppDbContext db,
                CancellationToken ct) =>
            {
                var handler = new GetOrdersQueryHandler(db);
                var result = await handler.Handle(truckId, ct);
                return Results.Ok(result.Data);
            })
            .WithTags("Orders")
            .WithOpenApi()
            .RequireAuthorization();
        }
    }
}
