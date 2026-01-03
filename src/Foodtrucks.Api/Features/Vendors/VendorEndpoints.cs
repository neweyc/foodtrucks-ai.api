using FluentValidation;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foodtrucks.Api.Features.Vendors
{
    // DTOs
    public class CreateVendorRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
    }

    public class UpdateVendorRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    // Validators
    public class CreateVendorValidator : AbstractValidator<CreateVendorRequest>
    {
        public CreateVendorValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(500);
            RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format");
            RuleFor(x => x.Website).Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("Invalid URL");
        }
    }

    public class UpdateVendorValidator : AbstractValidator<UpdateVendorRequest>
    {
        public UpdateVendorValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(500);
            RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format");
            RuleFor(x => x.Website).Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("Invalid URL");
        }
    }

    // Endpoints
    public class VendorEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/vendors")
                .WithTags("Vendors")
                .WithOpenApi()
                .RequireAuthorization();

            // GET /api/vendors
            group.MapGet("/", async (AppDbContext db) =>
            {
                return await db.Vendors.ToListAsync();
            });

            // GET /api/vendors/{id}
            group.MapGet("/{id}", async (int id, AppDbContext db) =>
            {
                var vendor = await db.Vendors.FindAsync(id);
                if (vendor == null) return Results.NotFound();
                return Results.Ok(vendor);
            });

            // POST /api/vendors
            group.MapPost("/", async (
                [FromBody] CreateVendorRequest request,
                AppDbContext db,
                IValidator<CreateVendorRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var vendor = new Vendor
                {
                    Name = request.Name,
                    Description = request.Description,
                    PhoneNumber = request.PhoneNumber,
                    Website = request.Website,
                    IsActive = true
                };

                db.Vendors.Add(vendor);
                await db.SaveChangesAsync();

                return Results.Created($"/api/vendors/{vendor.Id}", vendor);
            });

            // PUT /api/vendors/{id}
            group.MapPut("/{id}", async (
                int id,
                [FromBody] UpdateVendorRequest request,
                AppDbContext db,
                IValidator<UpdateVendorRequest> validator) =>
            {
                var validation = await validator.ValidateAsync(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                var vendor = await db.Vendors.FindAsync(id);
                if (vendor == null) return Results.NotFound();

                vendor.Name = request.Name;
                vendor.Description = request.Description;
                vendor.PhoneNumber = request.PhoneNumber;
                vendor.Website = request.Website;
                vendor.IsActive = request.IsActive;

                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            // DELETE /api/vendors/{id}
            group.MapDelete("/{id}", async (int id, AppDbContext db) =>
            {
                var vendor = await db.Vendors.FindAsync(id);
                if (vendor == null) return Results.NotFound();

                // Check for associated trucks
                bool hasTrucks = await db.Trucks.AnyAsync(t => t.VendorId == id);
                if (hasTrucks)
                {
                    return Results.Conflict("Cannot delete vendor with associated trucks.");
                }

                db.Vendors.Remove(vendor);
                await db.SaveChangesAsync();

                return Results.NoContent();
            });
        }
    }
}
