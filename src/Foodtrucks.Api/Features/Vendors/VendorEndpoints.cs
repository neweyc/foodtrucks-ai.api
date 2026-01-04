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
        public string Email { get; set; } = string.Empty;
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
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
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
                IValidator<CreateVendorRequest> validator,
                Foodtrucks.Api.Services.IPasswordHasher passwordHasher) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                // Check if email already exists
                var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingUser != null)
                {
                    return Results.Conflict($"User with email {request.Email} already exists.");
                }

                using var transaction = await db.Database.BeginTransactionAsync();
                try 
                {
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

                    var user = new Foodtrucks.Api.Features.Auth.User
                    {
                        Email = request.Email,
                        UserName = request.Email,
                        PasswordHash = passwordHasher.HashPassword("Password123!"),
                        VendorId = vendor.Id,
                        Role = "Vendor"
                    };

                    db.Users.Add(user);
                    await db.SaveChangesAsync();
                    
                    await transaction.CommitAsync();
                    return Results.Created($"/api/vendors/{vendor.Id}", vendor);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest($"Failed to create vendor/user: {ex.Message}");
                }
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
