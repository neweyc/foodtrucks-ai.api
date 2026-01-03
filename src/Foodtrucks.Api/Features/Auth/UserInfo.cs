using System.Security.Claims;
using Foodtrucks.Api.Routing;
using Microsoft.AspNetCore.Identity;

namespace Foodtrucks.Api.Features.Auth
{
    public class UserDto
    {
        public string Email { get; set; } = string.Empty;
        public int? VendorId { get; set; }
    }

    public class UserInfoEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/auth/me", async (ClaimsPrincipal user, UserManager<User> userManager) =>
            {
                var appUser = await userManager.GetUserAsync(user);
                if (appUser == null) return Results.Unauthorized();

                return Results.Ok(new UserDto
                {
                    Email = appUser.Email ?? "",
                    VendorId = appUser.VendorId
                });
            })
            .WithTags("Auth")
            .WithOpenApi()
            .RequireAuthorization();
        }
    }
}
