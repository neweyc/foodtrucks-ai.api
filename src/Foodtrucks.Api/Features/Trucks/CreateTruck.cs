using FluentValidation;
using Foodtrucks.Api.Commands;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Routing;
using Foodtrucks.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Features.Trucks
{
    public class CreateTruckRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
    }

    public class CreateTruckValidator : AbstractValidator<CreateTruckRequest>
    {
        public CreateTruckValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(500);
        }
    }

    public class CreateTruckCommandHandler(AppDbContext db, IValidator<CreateTruckRequest> validator, IVendorAuthorizationService authService)
    {
        public async Task<CommandResult> Handle(CreateTruckRequest request, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct)
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return CommandResult.Failure(validationResult.ToDictionary());
            }

            var vendorId = await authService.GetVendorIdAsync(user);
            if (vendorId == null)
            {
                return CommandResult.Failure("User is not associated with a vendor account.");
            }

            var truck = new Truck
            {
                VendorId = vendorId.Value,
                Name = request.Name,
                Description = request.Description,
                Schedule = request.Schedule,
                // Default props
                IsActive = false,
                CurrentLatitude = 0,
                CurrentLongitude = 0
            };

            db.Trucks.Add(truck);
            await db.SaveChangesAsync(ct);

            return CommandResult.SuccessResult(truck.Id, "Truck created successfully");
        }
    }

    public class CreateTruckEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/trucks", async (
                [FromBody] CreateTruckRequest request,
                System.Security.Claims.ClaimsPrincipal user,
                AppDbContext db,
                IValidator<CreateTruckRequest> validator,
                IVendorAuthorizationService authService,
                CancellationToken ct) =>
            {
                var handler = new CreateTruckCommandHandler(db, validator, authService);
                var result = await handler.Handle(request, user, ct);

                if (!result.Success)
                {
                   return Results.BadRequest(result.Message);
                }
                
                return Results.Created($"/api/trucks/{result.Id}", result);
            })
            .WithTags("Trucks")
            .WithOpenApi()
            .RequireAuthorization("VendorOnly");
        }
    }
}
