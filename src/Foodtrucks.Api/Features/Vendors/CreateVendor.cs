using FluentValidation;
using Foodtrucks.Api.Commands;
using Foodtrucks.Api.Data;
using Foodtrucks.Api.Routing;
using Microsoft.AspNetCore.Mvc;

namespace Foodtrucks.Api.Features.Vendors
{
    public class CreateVendorRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
    }

    public class CreateVendorValidator : AbstractValidator<CreateVendorRequest>
    {
        public CreateVendorValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(500);
            RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format");
            RuleFor(x => x.Website).Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).When(x => !string.IsNullOrEmpty(x.Website)).WithMessage("Invalid URL");
        }
    }

    public class CreateVendorCommandHandler(AppDbContext db, IValidator<CreateVendorRequest> validator)
    {
        public async Task<CommandResult> Handle(CreateVendorRequest request, CancellationToken ct)
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return CommandResult.Failure(validationResult.ToDictionary());
            }

            var vendor = new Vendor
            {
                Name = request.Name,
                Description = request.Description,
                PhoneNumber = request.PhoneNumber,
                Website = request.Website,
                IsActive = false
            };

            db.Vendors.Add(vendor);
            await db.SaveChangesAsync(ct);

            return CommandResult.SuccessResult(vendor.Id, "Vendor created successfully");
        }
    }

    public class CreateVendorEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/vendors", async (
                [FromBody] CreateVendorRequest request,
                AppDbContext db,
                IValidator<CreateVendorRequest> validator,
                CancellationToken ct) =>
            {
                var handler = new CreateVendorCommandHandler(db, validator);
                var result = await handler.Handle(request, ct);

                if (!result.Success)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]> { { "Error", new[] { result.Message } } });
                }

                return Results.Created($"/api/vendors/{result.Id}", result);
            })
            .WithTags("Vendors")
            .WithOpenApi()
            .RequireAuthorization();
        }
    }
}
