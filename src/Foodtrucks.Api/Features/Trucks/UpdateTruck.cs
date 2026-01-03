using FluentValidation;
using Foodtrucks.Api.Commands;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Routing;
using Foodtrucks.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Features.Trucks
{
    public record UpdateTruckRequest(string Name, string Description, string Schedule);

    public class UpdateTruckValidator : AbstractValidator<UpdateTruckRequest>
    {
        public UpdateTruckValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Description).NotEmpty();
            RuleFor(x => x.Schedule).NotEmpty();
        }
    }

    public class UpdateTruckCommandHandler(AppDbContext db, IVendorAuthorizationService authService)
    {
        public async Task<CommandResult> Handle(int truckId, UpdateTruckRequest request, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct)
        {
            var canManage = await authService.CanManageTruckAsync(user, truckId);
            if (!canManage)
            {
                return CommandResult.ForbiddenResult("You do not have permission to manage this truck.");
            }

            var truck = await db.Trucks.FindAsync(new object[] { truckId }, ct);

            if (truck == null)
            {
                return CommandResult.NotFoundResult($"Truck {truckId} not found");
            }

            truck.Name = request.Name;
            truck.Description = request.Description;
            truck.Schedule = request.Schedule;

            await db.SaveChangesAsync(ct);

            return CommandResult.SuccessResult();
        }
    }

    public class UpdateTruckEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/api/trucks/{id}", async (
                [Microsoft.AspNetCore.Mvc.FromRoute] int id, 
                [Microsoft.AspNetCore.Mvc.FromBody] UpdateTruckRequest request, 
                System.Security.Claims.ClaimsPrincipal user, 
                AppDbContext db,
                IVendorAuthorizationService authService,
                CancellationToken ct) =>
            {
                var handler = new UpdateTruckCommandHandler(db, authService);
                var result = await handler.Handle(id, request, user, ct);

                if (result.IsNotFound)
                {
                    return Results.NotFound(result.Message);
                }

                if (result.IsForbidden)
                {
                    return Results.Forbid();
                }

                if (!result.IsSuccess)
                {
                    return Results.BadRequest(result.Message);
                }

                return Results.NoContent();
            })
            .WithTags("Trucks")
            .RequireAuthorization();
        }
    }
}
