using Foodtrucks.Api.Data;
using Foodtrucks.Api.Services;
using Foodtrucks.Api.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Foodtrucks.Api.Features.Vendors
{
    public class VendorConnectEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/vendors/{id}/onboarding")
                           .WithTags("Vendor Connect");

            group.MapPost("/", async (
                int id,
                AppDbContext db,
                IStripeService stripeService,
                ILogger<VendorConnectEndpoints> logger,
                CancellationToken ct) =>
            {
                var vendor = await db.Vendors.FindAsync(new object[] { id }, ct);
                if (vendor == null) return Results.NotFound("Vendor not found");

                try 
                {
                    // Create Account if not exists
                    if (string.IsNullOrEmpty(vendor.StripeAccountId))
                    {
                        var accountId = await stripeService.CreateVendorAccountAsync(vendor.Name, vendor.Id);
                        vendor.StripeAccountId = accountId;
                        await db.SaveChangesAsync(ct);
                    }

                    // Create Account Link for onboarding
                    var url = await stripeService.CreateAccountLinkAsync(vendor.StripeAccountId, vendor.Id);
                    return Results.Ok(new { url });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error creating vendor account link");
                    return Results.Problem("Failed to initiate onboarding");
                }
            });

            group.MapGet("/return", async (
                int id,
                AppDbContext db,
                IStripeService stripeService,
                CancellationToken ct) =>
            {
                // This is where Stripe redirects back to.
                // We should check if the account involves details_submitted
                var vendor = await db.Vendors.FindAsync(new object[] { id }, ct);
                if (vendor == null || string.IsNullOrEmpty(vendor.StripeAccountId)) return Results.NotFound();
                
                var isComplete = await stripeService.IsAccountCompleteAsync(vendor.StripeAccountId);
                
                // We could redirect to a specific UI page based on status
               return Results.Redirect($"/vendor/profile?onboarding={(isComplete ? "success" : "incomplete")}");
            });
        }
    }
}
