using Foodtrucks.Api.Commands;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Routing;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Features.Trucks
{
    public record GetTrucksRequest(int? VendorId);

    public class GetTrucksQueryHandler(AppDbContext db)
    {
        public async Task<CommandResult<List<Truck>>> Handle(int? vendorId, CancellationToken ct)
        {
            var query = db.Trucks.AsNoTracking();

            if (vendorId.HasValue)
            {
                query = query.Where(t => t.VendorId == vendorId.Value);
            }

            var trucks = await query.ToListAsync(ct);
            return CommandResult<List<Truck>>.SuccessResult(trucks);
        }
    }

    public class GetTruckQueryHandler(AppDbContext db)
    {
        public async Task<CommandResult<Truck>> Handle(int id, CancellationToken ct)
        {
             var truck = await db.Trucks
                .AsNoTracking()
                .Include(t => t.MenuCategories)
                .ThenInclude(mc => mc.MenuItems)
                    .ThenInclude(mi => mi.Sizes)
                .Include(t => t.MenuCategories)
                .ThenInclude(mc => mc.MenuItems)
                    .ThenInclude(mi => mi.Options)
                .FirstOrDefaultAsync(t => t.Id == id, ct);

            if (truck == null)
            {
                return CommandResult<Truck>.NotFoundResult($"Truck {id} not found");
            }
            return CommandResult<Truck>.SuccessResult(truck);
        }
    }

    public class GetTrucksEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/trucks", async ([AsParameters] GetTrucksRequest request, AppDbContext db, CancellationToken ct) =>
            {
                var handler = new GetTrucksQueryHandler(db);
                var result = await handler.Handle(request.VendorId, ct);
                return Results.Ok(result.Data);
            })
            .WithTags("Trucks")
            .WithOpenApi();

            app.MapGet("/api/trucks/{id}", async (int id, AppDbContext db, CancellationToken ct) =>
            {
                var handler = new GetTruckQueryHandler(db);
                var result = await handler.Handle(id, ct);
                
                if (result.IsNotFound)
                {
                    return Results.NotFound(result.Message);
                }

                return Results.Ok(result.Data);
            })
            .WithTags("Trucks")
            .WithOpenApi();
        }
    }
}
