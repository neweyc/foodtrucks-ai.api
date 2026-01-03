using FluentValidation;
using Foodtrucks.Api.Commands;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Features.Menu
{
    public class AddMenuItemRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string PhotoUrl { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;

        public List<MenuItemSizeDto> Sizes { get; set; } = new();
        public List<MenuItemOptionDto> Options { get; set; } = new();
    }

    public class MenuItemSizeDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class MenuItemOptionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class AddMenuItemValidator : AbstractValidator<AddMenuItemRequest>
    {
        public AddMenuItemValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Price).GreaterThan(0);
        }
    }

    public class AddMenuItemCommandHandler(AppDbContext db, IValidator<AddMenuItemRequest> validator)
    {
        public async Task<CommandResult> Handle(int categoryId, AddMenuItemRequest request, CancellationToken ct)
        {
            try
            {
                var validationResult = await validator.ValidateAsync(request, ct);
                if (!validationResult.IsValid)
                {
                    return CommandResult.Failure(validationResult.ToDictionary());
                }

                var categoryExists = await db.MenuCategories.AnyAsync(c => c.Id == categoryId, ct);
                if (!categoryExists)
                {
                    return CommandResult.NotFoundResult("Category not found");
                }

                var item = new MenuItem
                {
                    MenuCategoryId = categoryId,
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    PhotoUrl = request.PhotoUrl,
                    IsAvailable = request.IsAvailable,
                    Sizes = request.Sizes?.Select(s => new MenuItemSize 
                    { 
                        Name = s.Name, 
                        Price = s.Price 
                    }).ToList() ?? new(),
                    Options = request.Options?.Select(o => new MenuItemOption
                    {
                        Name = o.Name,
                        Section = o.Section,
                        Price = o.Price
                    }).ToList() ?? new()
                };

                db.MenuItems.Add(item);
                await db.SaveChangesAsync(ct);

                return CommandResult.SuccessResult(item.Id, "Menu Item added successfully");
            }
            catch (Exception ex)
            {
                return CommandResult.Failure($"EXCEPTION: {ex.Message} {ex.InnerException?.Message}");
            }
        }
    }

    public class AddMenuItemEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/menu-categories/{categoryId}/items", async (
                int categoryId,
                [FromBody] AddMenuItemRequest request,
                AppDbContext db,
                IValidator<AddMenuItemRequest> validator,
                CancellationToken ct) =>
            {
                var handler = new AddMenuItemCommandHandler(db, validator);
                var result = await handler.Handle(categoryId, request, ct);

                if (result.Success)
                {
                    return Results.Created($"/api/menu-items/{result.Id}", result);
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
