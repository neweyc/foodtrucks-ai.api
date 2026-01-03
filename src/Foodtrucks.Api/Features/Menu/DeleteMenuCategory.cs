using Foodtrucks.Api.Commands;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Features.Menu
{
    public class DeleteMenuCategoryCommandHandler(AppDbContext db)
    {
        public async Task<CommandResult> Handle(int truckId, int categoryId, CancellationToken ct)
        {
            var category = await db.MenuCategories
                .Where(c => c.Id == categoryId && c.TruckId == truckId)
                .FirstOrDefaultAsync(ct);

            if (category == null)
            {
                return CommandResult.NotFoundResult("Menu Category not found or does not belong to this truck");
            }

            // Optional: Check if items exist and prevent deletion if strict, 
            // but for now we'll allow cascade deletion if configured in DB or explicit removal.
            // EF Core default behavior depends on configuration. 
            // If explicit removal of child items is needed:
            // var items = await db.MenuItems.Where(i => i.MenuCategoryId == categoryId).ToListAsync(ct);
            // db.MenuItems.RemoveRange(items);
            
            db.MenuCategories.Remove(category);
            await db.SaveChangesAsync(ct);

            return CommandResult.SuccessResult(categoryId, "Menu Category deleted successfully");
        }
    }

    public class DeleteMenuCategoryEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/api/trucks/{truckId}/menu-categories/{categoryId}", async (
                int truckId,
                int categoryId,
                AppDbContext db,
                CancellationToken ct) =>
            {
                var handler = new DeleteMenuCategoryCommandHandler(db);
                var result = await handler.Handle(truckId, categoryId, ct);

                if (result.Success)
                {
                    return Results.NoContent();
                }

                if (result.IsNotFound)
                {
                    return Results.NotFound(result.Message);
                }

                return Results.BadRequest(result.Message);
            })
            .WithTags("Menu")
            .WithOpenApi()
            .RequireAuthorization();
        }
    }
}
