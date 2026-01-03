using FluentValidation;
using Foodtrucks.Api.Commands;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Features.Menu
{
    public class AddMenuCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class AddMenuCategoryValidator : AbstractValidator<AddMenuCategoryRequest>
    {
        public AddMenuCategoryValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        }
    }

    public class AddMenuCategoryCommandHandler(AppDbContext db, IValidator<AddMenuCategoryRequest> validator)
    {
        public async Task<CommandResult> Handle(int truckId, AddMenuCategoryRequest request, CancellationToken ct)
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return CommandResult.Failure(validationResult.ToDictionary());
            }

            var truckExists = await db.Trucks.AnyAsync(t => t.Id == truckId, ct);
            if (!truckExists)
            {
                return CommandResult.NotFoundResult("Truck not found");
            }

            var category = new MenuCategory
            {
                TruckId = truckId,
                Name = request.Name
            };

            db.MenuCategories.Add(category);
            await db.SaveChangesAsync(ct);

            return CommandResult.SuccessResult(category.Id, "Menu Category added successfully");
        }
    }

    public class AddMenuCategoryEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/trucks/{truckId}/menu-categories", async (
                int truckId,
                [FromBody] AddMenuCategoryRequest request,
                AppDbContext db,
                IValidator<AddMenuCategoryRequest> validator,
                CancellationToken ct) =>
            {
                var handler = new AddMenuCategoryCommandHandler(db, validator);
                var result = await handler.Handle(truckId, request, ct);

                if (result.Success)
                {
                     // Ideally we return the created object or a DTO, but for now CommandResult only has ID. 
                     // We can construct a minimal object or fetch it if needed, but for creation usually we return the resource.
                     // Refactoring CommandResult to be consistent. 
                     // The previous code returned the entity 'category'. 
                     // Let's assume we want to return the entity data. 
                     // Need to change CommandResult to CommandResult<T> for Create operations too if we want to return data.
                     // Or just return Ok/Created with ID. 
                     // The existing pattern in CreateVendor returned the vendor object. 
                     // My CommandResult.SuccessResult(id, msg) doesn't return the object data.
                     // I should probably use CommandResult<T> for creations if I want to return the object.
                     // For now, I'll stick to returning what I have or re-fetch/construct.
                     // To match previous behavior strictly, I should return the category object.
                     
                     // Let's use the ID to create a response or if I changed the handler to return CommandResult<MenuCategory> it would be better.
                     // But for this step I will follow the pattern I used in CreateTruck/Vendor where I just returned result.Id in some cases or result. 
                     // Wait, CreateVendor returned `result` which is `CommandResult`. The original code returned `vendor`.
                     // This is a slight behavior change. The user asked to "observe command pattern in PlaceOrder.cs". 
                     // PlaceOrder returned `CommandResult.SuccessResult(order.Id, ...)` and Endpoint returned `Results.Ok($"/api/orders/{result.Id}")`.
                     // So returning just the ID/Location is the requested pattern. 
                     
                     return Results.Created($"/api/trucks/{truckId}/menu-categories/{result.Id}", result);
                }

                if (result.IsNotFound)
                {
                    return Results.NotFound(result.Message);
                }

                return Results.ValidationProblem(new Dictionary<string, string[]> { { "Error", new[] { result.Message } } });
            })
            .WithTags("Menu")
            .WithOpenApi()
            .RequireAuthorization();
        }
    }
}
