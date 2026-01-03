using FluentValidation;
using Foodtrucks.Api.Commands;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Features.Menu
{
    public class EditMenuItemRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string PhotoUrl { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;

        public List<MenuItemSizeDto> Sizes { get; set; } = new();
        public List<MenuItemOptionDto> Options { get; set; } = new();
    }

    public class EditMenuItemValidator : AbstractValidator<EditMenuItemRequest>
    {
        public EditMenuItemValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Price).GreaterThan(0);
        }
    }

    public class EditMenuItemCommandHandler(AppDbContext db, IValidator<EditMenuItemRequest> validator)
    {
        public async Task<CommandResult> Handle(int itemId, EditMenuItemRequest request, CancellationToken ct)
        {
            try
            {
                var validationResult = await validator.ValidateAsync(request, ct);
                if (!validationResult.IsValid)
                {
                    return CommandResult.Failure(validationResult.ToDictionary());
                }

                var item = await db.MenuItems
                    .Include(i => i.Sizes)
                    .Include(i => i.Options)
                    .FirstOrDefaultAsync(i => i.Id == itemId, ct);

                if (item == null)
                {
                    return CommandResult.NotFoundResult("Menu Item not found");
                }

                // Update scalar properties
                item.Name = request.Name;
                item.Description = request.Description;
                item.Price = request.Price;
                item.PhotoUrl = request.PhotoUrl;
                item.IsAvailable = request.IsAvailable;

                // Replace Sizes
                db.Set<MenuItemSize>().RemoveRange(item.Sizes);
                item.Sizes = request.Sizes?.Select(s => new MenuItemSize 
                { 
                    MenuItemId = itemId,
                    Name = s.Name, 
                    Price = s.Price 
                }).ToList() ?? new();

                // Replace Options
                db.Set<MenuItemOption>().RemoveRange(item.Options);
                item.Options = request.Options?.Select(o => new MenuItemOption
                {
                    MenuItemId = itemId,
                    Name = o.Name,
                    Section = o.Section,
                    Price = o.Price
                }).ToList() ?? new();

                await db.SaveChangesAsync(ct);

                return CommandResult.SuccessResult(item.Id, "Menu Item updated successfully");
            }
            catch (Exception ex)
            {
                return CommandResult.Failure($"EXCEPTION: {ex.Message} {ex.InnerException?.Message}");
            }
        }
    }

    public class EditMenuItemEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/api/menu-items/{itemId}", async (
                int itemId,
                [FromBody] EditMenuItemRequest request,
                AppDbContext db,
                IValidator<EditMenuItemRequest> validator,
                CancellationToken ct) =>
            {
                var handler = new EditMenuItemCommandHandler(db, validator);
                var result = await handler.Handle(itemId, request, ct);

                if (result.Success)
                {
                    return Results.Ok(result);
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
